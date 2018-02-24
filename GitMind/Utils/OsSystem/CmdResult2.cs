namespace GitMind.Utils.OsSystem
{
	public class CmdResult2
	{
		private static readonly char[] Eol = "\n".ToCharArray();


		public CmdResult2(
			string command, string arguments, int exitCode, string output, string error)
		{
			Command = command;
			Arguments = arguments;
			Output = output;
			Error = error;
			ExitCode = exitCode;
		}

		public string Command { get; }

		public string Arguments { get; }

		public int ExitCode { get; }

		public string Output { get; }

		public string Error { get; }

		public static implicit operator string(CmdResult2 result2) => result2.Output;


		public override string ToString() => $"{Command} {Arguments}{ExitText}{OutputText}{ErrorText}";

		private string ExitText => ExitCode == 0 ? "" : $"\nExit code: {ExitCode}";
		private string OutputText => string.IsNullOrEmpty(Output) ? "" : $"\n{Truncate(Output)}";
		private string ErrorText => string.IsNullOrEmpty(Error) ? "" : $"\nError:\n{Truncate(Error)}";

		private static string Truncate(string text)
		{
			if (text == null)
			{
				return string.Empty;
			}
			else
			{
				int maxRows = 5;
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