using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;


namespace GitMind.Utils
{
	internal static class Log
	{
		private static readonly UdpClient UdpClient = new UdpClient();
		private static readonly IPEndPoint LocalLogEndPoint = new IPEndPoint(IPAddress.Loopback, 40000);


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

				Debugger.Log(0, Debugger.DefaultCategory, text);
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Debugger.Log(0, Debugger.DefaultCategory, "ERROR Failed to log to udp " + e);
			}
		}
	}
}