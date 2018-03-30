﻿using System;
using System.Collections.Generic;
using System.Threading;


namespace GitMind.Utils.OsSystem
{
	public class CmdResult2
	{
		private static readonly char[] Eol = "\n".ToCharArray();


		public CmdResult2(string command,
			string arguments,
			int exitCode,
			string output,
			string error,
			TimeSpan elapsed,
			CancellationToken ct)
		{
			Command = command;
			Arguments = arguments;
			Output = output;
			Error = error;
			Elapsed = elapsed;
			IsCanceled = ct.IsCancellationRequested;
			ExitCode = exitCode;
		}

		public string Command { get; }

		public string Arguments { get; }

		public int ExitCode { get; }

		public string Output { get; }

		public IReadOnlyList<string> OutputLines => Output.Split(Eol);

		public string Error { get; }

		public TimeSpan Elapsed { get; }
		public long ElapsedMs => (long)Elapsed.TotalMilliseconds;

		public IReadOnlyList<string> ErrorLines => Error.Split(Eol);

		public bool IsCanceled { get; }

		public static implicit operator string(CmdResult2 result2) => result2.Output;

		public void ThrowIfError(string message)
		{
			if (ExitCode != 0 && !IsCanceled)
			{
				string errorText = $"{message},\n{this}";
				ApplicationException e = new ApplicationException(errorText);
				Log.Exception(e);
				throw e;
			}
		}

		public override string ToString() => $"{Command} {Arguments}{ExitText}{OutputText}{ErrorText}";

		public string ToStringShort() => $"{Command} {Arguments}{ShortExit}";


		private string ExitText => ExitCode == 0 ? "" : $"\nExit code: {ExitCode}";
		private string ShortExit => ExitCode == 0 ? "" : $"\nExit code: {ExitCode}{ErrorText}";
		private string OutputText => string.IsNullOrEmpty(Output) ? "" : $"\n{Truncate(Output)}";
		private string ErrorText => string.IsNullOrEmpty(Error) ? "" :
				ExitCode == 0 ? $"\nProgress:\n{Truncate(Error)}" : $"\nError:\n{Truncate(Error)}";


		private static string Truncate(string text)
		{
			if (text == null)
			{
				return string.Empty;
			}
			else
			{
				int maxRows = 4;
				string[] rows = text.Split(Eol);
				if (rows.Length > maxRows)
				{
					return $"{string.Join("\n", rows, 0, maxRows)} \n... ({rows.Length} lines)";
				}
				else
				{
					return text;
				}
			}
		}
	}
}