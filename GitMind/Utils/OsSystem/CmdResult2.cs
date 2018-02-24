namespace GitMind.Utils.OsSystem
{
	public class CmdResult2
	{
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

		public override string ToString() =>
			$"Command: {Command} {Arguments}\n"+
			$"Exit code: {ExitCode}\nOutput: {Truncate(Output)}\nError: {Truncate(Error)}";


		private static string Truncate(string text)
		{
			if (text == null)
			{
				return string.Empty;
			}
			else
			{
				string[] rows = text.Split("\n".ToCharArray());
				if (rows.Length > 10)
				{
					return string.Join("\n", rows, 0, 10) + "\n[...]";
				}
				else
				{
					return text;
				}
			}
		}
	}
}