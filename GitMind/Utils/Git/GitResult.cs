using System.Collections.Generic;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git
{
	public class GitResult
	{
		private readonly CmdResult2 cmdResult;
		
		public GitResult(CmdResult2 cmdResult) => this.cmdResult = cmdResult;

		public bool IsOk => ExitCode == 0;

		public int ExitCode => cmdResult.ExitCode;
		public string Output => cmdResult.Output;
		public IReadOnlyList<string> OutputLines => cmdResult.OutputLines;
		public string Error => cmdResult.Error;
		public bool IsCanceled => cmdResult.IsCanceled;

		public static implicit operator string(GitResult result) => result.Output;

		public void ThrowIfError(string message) => cmdResult.ThrowIfError(message);

		public override string ToString() => cmdResult.ToString();
		public string ToStringShort() => cmdResult.ToStringShort();
	}
}