using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.Git
{
	internal interface IGitNetworkService
	{
		Task<R> FetchAsync();

		Task<R> FetchBranchAsync(BranchName branchName);

		Task<R> FetchRefsAsync(IEnumerable<string> refspecs);

		Task<R> PushCurrentBranchAsync();

		Task<R> PushBranchAsync(BranchName branchName);

		Task<R> PushRefsAsync(IEnumerable<string> refspecs);

		Task<R> PublishBranchAsync(BranchName branchName);

		Task<R> DeleteRemoteBranchAsync(BranchName branchName);
	}
}