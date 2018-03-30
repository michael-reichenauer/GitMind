using System;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git.Private
{
	internal interface IGitCmd
	{
		Task<GitResult> RunAsync(string gitArgs, CancellationToken ct);
		
		Task<GitResult> RunAsync(
			string gitArgs, Action<string> outputLines, CancellationToken ct);

		Task<GitResult> RunAsync(
			string gitArgs, GitOptions options, CancellationToken ct);
	}
}