﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Common.Tracking;


namespace GitMind.Utils
{
	internal static class Log
	{
		private static readonly int MaxLogFileSize = 2000000;

		private static readonly UdpClient UdpClient = new UdpClient();
		private static readonly BlockingCollection<string> logTexts = new BlockingCollection<string>();

		private static readonly IPEndPoint usageLogEndPoint =
			new IPEndPoint(IPAddress.Parse("10.85.12.4"), 41110);

		private static readonly object syncRoot = new object();

		private static readonly string LogPath = ProgramPaths.GetLogFilePath();
		private static readonly int ProcessID = Process.GetCurrentProcess().Id;
		private static readonly string LevelUsage = "USAGE";
		private static readonly string LevelDebug = "DEBUG";
		private static readonly string LevelInfo = "INFO ";
		private static readonly string LevelWarn = "WARN ";
		private static readonly string LevelError = "ERROR";
		private static readonly Lazy<bool> DisableErrorAndUsageReporting;

		static Log()
		{
			DisableErrorAndUsageReporting = new Lazy<bool>(() =>
				Settings.Get<Options>().DisableErrorAndUsageReporting);
		
			Task.Factory.StartNew(SendBufferedLogRows, TaskCreationOptions.LongRunning)
				.RunInBackground();
		}


		private static void SendBufferedLogRows()
		{
			while (!logTexts.IsCompleted)
			{
				string logText = logTexts.Take();

				Native.OutputDebugString(logText);

				//Debugger.Log(0, Debugger.DefaultCategory, logText);
			}
		}


		public static void Usage(
			string msg,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			Write(LevelUsage, msg, memberName, sourceFilePath, sourceLineNumber);
		}

		public static void Debug(
			string msg,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			Write(LevelDebug, msg, memberName, sourceFilePath, sourceLineNumber);
		}

		public static void Info(
			string msg,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			Write(LevelInfo, msg, memberName, sourceFilePath, sourceLineNumber);
		}


		public static void Warn(
			string msg,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			Write(LevelWarn, msg, memberName, sourceFilePath, sourceLineNumber);
			Track.TraceWarn($"{sourceFilePath}-{memberName}({sourceLineNumber}) {msg}");
		}


		public static void Error(
			string msg,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			Write(LevelError, msg, memberName, sourceFilePath, sourceLineNumber);
			Track.TraceError($"{sourceFilePath}-{memberName}({sourceLineNumber}) {msg}");
		}

		public static void Exception(
			Exception e,
			string msg,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			Write(LevelError, $"{msg}\n{e}", memberName, sourceFilePath, sourceLineNumber);
			string message = $"{sourceFilePath}-{memberName} - {e.Message}, {msg}";
			Track.Exception(e, message);
		}


		private static void Write(
			string level,
			string msg,
			string memberName,
			string filePath,
			int lineNumber,
			[CallerFilePath] string sourceFilePath = "")
		{
			int prefixLength = sourceFilePath.Length - 20;
			filePath = filePath.Substring(prefixLength);
			string text = $"{level} [{ProcessID}] {filePath}({lineNumber}) {memberName} - {msg}";

			if (level == LevelUsage || level == LevelWarn || level == LevelError)
			{
				SendUsage(text);
			}

			try
			{
				//byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
				//UdpClient.Send(bytes, bytes.Length, LocalLogEndPoint);
				SendLog(text);
				WriteToFile(text);			
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				//Debugger.Log(0, Debugger.DefaultCategory, "ERROR Failed to log to udp " + e);
				SendLog("ERROR Failed to log to udp " + e);
			}
		}

		
		private static void SendLog(string text)
		{
			logTexts.Add(text);
		}



		private static void SendUsage(string text)
		{
			if (DisableErrorAndUsageReporting.Value)
			{
				return;
			}

			try
			{
				string logRow = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff} [{ProcessID}] {text}";

				byte[] bytes = System.Text.Encoding.UTF8.GetBytes(logRow);
				UdpClient.Send(bytes, bytes.Length, usageLogEndPoint);
			}
			catch (Exception)
			{		
				// Ignore failed
			}
		}


		private static void WriteToFile(string text)
		{
			lock (syncRoot)
			{
				Exception error = null;
				for (int i = 0; i < 10; i++)
				{
					try
					{
						File.AppendAllText(
							LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss,fff} [{ProcessID}] {text}{Environment.NewLine}");

						long length = new FileInfo(LogPath).Length;

						if (length > MaxLogFileSize)
						{
							MoveLargeLogFile();
						}

						return;
					}
					catch (DirectoryNotFoundException)
					{
						// Ignore error since folder has been deleted during uninstallation
						return;
					}
					catch (Exception e)
					{
						Thread.Sleep(30);
						error = e;
					}
				}

				SendLog("ERROR Failed to log to file: " + error);
			}		
		}


		private static void MoveLargeLogFile()
		{
			try
			{
				string tempPath = LogPath + "." + Guid.NewGuid();
				File.Move(LogPath, tempPath);

				Task.Run(() =>
				{
					try
					{
						string secondLogFile = LogPath + ".2.log";
						if (File.Exists(secondLogFile))
						{
							File.Delete(secondLogFile);
						}

						File.Move(tempPath, secondLogFile);
					}
					catch (Exception e)
					{
						SendLog("ERROR Failed to move temp to second log file: " + e);
					}
					
				}).RunInBackground();
			}
			catch (Exception e)
			{
				SendLog("ERROR Failed to move large log file: " + e);
			}	
		}

		private static class Native
		{
			[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
			public static extern void OutputDebugString(string message);
		}
	}
}