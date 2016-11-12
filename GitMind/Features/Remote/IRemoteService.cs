using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.Features.Remote
{
	internal interface IRemoteService
	{
		Task<R> FetchAsync();
		Task<R> PushBranchAsync(BranchName branchName);
		Task PushNotesAsync(string rootId);
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