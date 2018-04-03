using System;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal interface IGitCmd
	{
		Task<CmdResult2> RunAsync(string gitArgs, CancellationToken ct);

		Task<CmdResult2> RunAsync(
			string gitArgs, Action<string> outputLines, CancellationToken ct);

		Task<CmdResult2> RunAsync(
			string gitArgs, GitOptions options, CancellationToken ct);
	}
}