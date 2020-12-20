using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind.Features.Remote
{
	internal interface IRemoteService
	{
		Task<R> FetchAsync();
		Task<R> PushBranchAsync(BranchName branchName);
		Task PushNotesAsync(CommitSha rootId);
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