using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.Features.Remote
{
	internal interface IRemoteService
	{
		Task<R> FetchAsync();
		Task<R> PushBranchAsync(BranchName branchName);
		Task PushNotesAsync(CommitId rootId);
		Task<R> FetchAllNotesAsync();
		bool CanExecuteTryUpdateAllBranches();
		Task TryUpdateAllBranchesAsync();
		Task PullCurrentBranchAsync();
		bool CanExecutePullCurrentBranch();
		Task PushCurrentBranchAsync();
		bool CanExecutePushCurrentBranch();
		Task TryPushAllBranchesAsync();
		bool CanExecuteTryPushAllBranches();
	}
}