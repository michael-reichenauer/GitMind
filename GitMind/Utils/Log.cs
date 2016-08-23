using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Settings;


namespace GitMind.Utils
{
	internal static class Log
	{
		private static readonly int MaxLOgFileSize = 10000000;

		private static readonly UdpClient UdpClient = new UdpClient();
		private static readonly IPEndPoint LocalLogEndPoint = new IPEndPoint(IPAddress.Loopback, 40000);

		private static readonly string LogPath = ProgramPaths.GetLogFilePath();


		public static void Debug(
			string msg,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			Write("DEBUG [", msg, memberName, sourceFilePath, sourceLineNumber);
		}


		public static void Warn(
			string msg,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			Write("WARN  [", msg, memberName, sourceFilePath, sourceLineNumber);
		}


		internal static void Warn(
			Exception e,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			throw new NotImplementedException();
		}


		public static void Error(
			string msg,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			Write("ERROR [", msg, memberName, sourceFilePath, sourceLineNumber);
		}


		private static void Write(
			string level,
			string msg,
			string memberName,
			string filePath,
			int lineNumber)
		{
			string text = $"{level} {filePath}({lineNumber}) {memberName} - {msg}";

			try
			{
				byte[] bytes = System.Text.Encoding.UTF8.GetBytes(text);
				UdpClient.Send(bytes, bytes.Length, LocalLogEndPoint);

				WriteToFile(text);

				//Debugger.Log(0, Debugger.DefaultCategory, text);
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				//Debugger.Log(0, Debugger.DefaultCategory, "ERROR Failed to log to udp " + e);
				OutputDebugString("ERROR Failed to log to udp " + e);
			}
		}


		private static void WriteToFile(string text)
		{
			Exception error = null;
			for (int i = 0; i < 10; i++)
			{
				try
				{
					File.AppendAllText(LogPath, text + "\n");

					long length = new FileInfo(LogPath).Length;

					if (length > MaxLOgFileSize)
					{
						MoveLargeLogFile();
					}

					return;
				}
				catch (Exception e)
				{
					Thread.Sleep(10);
					error = e;
				}
			}

			OutputDebugString("ERROR Failed to log to file: " + error);
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
						OutputDebugString("ERROR Failed to move temp to second log file: " + e);
					}
					
				}).RunInBackground();
			}
			catch (Exception e)
			{
				OutputDebugString("ERROR Failed to move large log file: " + e);
			}	
		}


		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern void OutputDebugString(string message);
	}
}