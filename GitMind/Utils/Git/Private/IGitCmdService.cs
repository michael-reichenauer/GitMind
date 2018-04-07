using System;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	public interface IGitCmdService
	{
		Task<R<CmdResult2>> RunAsync(string gitArgs, CancellationToken ct);

		Task<R<CmdResult2>> RunAsync(
			string gitArgs, Action<string> outputLines, CancellationToken ct);

		Task<R<CmdResult2>> RunWithProgressAsync(string gitArgs, Action<string> lines, CancellationToken ct);

		Task<R<CmdResult2>> RunAsync(
			string gitArgs, GitOptions options, CancellationToken ct);


		Task<CmdResult2> RunCmdAsync(string gitArgs, CancellationToken ct);
	}
}