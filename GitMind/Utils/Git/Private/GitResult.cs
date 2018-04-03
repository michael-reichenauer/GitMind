using System;
using System.Collections.Generic;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	public class GitResult
	{
		private readonly CmdResult2 cmdResult;

		public GitResult(CmdResult2 cmdResult) => this.cmdResult = cmdResult;

		public bool IsOk => ExitCode == 0;
		public bool IsFaulted => !IsOk;
		public int ExitCode => cmdResult.ExitCode;
		public string Output => cmdResult.Output;
		public IEnumerable<string> OutputLines => cmdResult.OutputLines;
		public string Error => cmdResult.Error;
		public bool IsCanceled => cmdResult.IsCanceled;

		public bool IsAuthenticationFailed =>
			ExitCode == 128 && -1 != Error.IndexOfOic("Authentication failed");

		public static implicit operator string(GitResult result) => result.Output;

		public void ThrowIfError(string message) => cmdResult.ThrowIfError(message);

		public override string ToString() => cmdResult.ToString();
		public string ToStringShort() => cmdResult.ToStringShort();
	}
}