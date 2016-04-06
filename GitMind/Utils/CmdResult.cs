using System.Collections.Generic;


namespace GitMind.Utils
{
	public class CmdResult
	{
		public int ExitCode { get; }
		public IReadOnlyList<string> Output { get; }
		public IReadOnlyList<string> Error { get; }


		public CmdResult(int exitCode, IReadOnlyList<string> output, IReadOnlyList<string> error)
		{
			ExitCode = exitCode;
			Output = output;
			Error = error;
		}
	}
}