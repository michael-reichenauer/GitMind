using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace GitMind.Utils.OsSystem
{
	public class CmdResult2
	{
		public CmdResult2(
			string command,
			string arguments,
			string workingDirectory,
			int exitCode,
			string output,
			string error,
			TimeSpan elapsed,
			CancellationToken ct)
		{
			Command = command;
			Arguments = arguments;
			WorkingDirectory = workingDirectory;
			Output = output;
			Error = error;
			Elapsed = elapsed;
			IsCanceled = ct.IsCancellationRequested;
			ExitCode = exitCode;
		}

		public string Command { get; }
		public string Arguments { get; }
		public string WorkingDirectory { get; }
		public int ExitCode { get; }
		public string Output { get; }
		public string Error { get; }
		public TimeSpan Elapsed { get; }
		public long ElapsedMs => (long)Elapsed.TotalMilliseconds;
		public bool IsCanceled { get; }
		public bool IsOk => ExitCode == 0;
		public bool IsFaulted => !IsOk;
		public IEnumerable<string> OutputLines => Lines(Output);
		public IEnumerable<string> ErrorLines => Lines(Error);

		public static implicit operator string(CmdResult2 result2) => result2.Output;

		public static implicit operator R(CmdResult2 result) => result.IsOk ? R.Ok : Utils.Error.From(result);

		public void ThrowIfError(string message)
		{
			if (ExitCode != 0)
			{
				string errorText = $"{message},\n{this}";
				ApplicationException e = new ApplicationException(errorText);
				Log.Exception(e);
				throw e;
			}
		}

		public override string ToString() =>
			$"{Command} {Arguments}{ExitText}{OutputText}{ErrorText}";

		private string ExitText => ExitCode == 0 ? "" : $"\nExit code: {ExitCode}";
		private string OutputText => string.IsNullOrEmpty(Output) ? "" : $"\n{Truncate(Output)}";
		private string ErrorText => string.IsNullOrEmpty(Error) ? "" : ExitCode == 0 ?
			$"\nProgress:\n{Truncate(Error)}" : $"\nError:\n{Truncate(Error)}\nin: {WorkingDirectory}";


		private static IEnumerable<string> Lines(string text)
		{
			using (System.IO.StringReader reader = new System.IO.StringReader(text))
			{
				while (true)
				{
					string line = reader.ReadLine();
					if (line == null)
					{
						yield break;
					}

					yield return line;
				}
			}
		}

		private static string Truncate(string text)
		{
			if (text == null)
			{
				return string.Empty;
			}
			else
			{
				int maxRows = 10;
				string subText = string.Join("\n", Lines(text).Take(maxRows));
				if (subText.Length + maxRows < text.Length)
				{
					subText += "\n...";
				}

				return subText;
			}
		}
	}
}