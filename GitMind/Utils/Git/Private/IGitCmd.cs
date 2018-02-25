using System;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	public interface IGitCmd
	{
		Task<CmdResult2> RunAsync(string args, CancellationToken ct);


		Task<CmdResult2> RunAsync(
			string args, Action<string> outputLines, CancellationToken ct);
	}
}