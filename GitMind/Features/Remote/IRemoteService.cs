using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.Features.Remote
{
	internal interface IRemoteService
	{
		Task<R> FetchAsync();
		Task<R> FetchBranchAsync(BranchName branchName);
		Task<R> PushCurrentBranchAsync();
		Task<R> PushBranchAsync(BranchName branchName);
		Task PushNotesAsync(string rootId);
		Task<R> FetchAllNotesAsync();
		bool CanExecuteTryUpdateAllBranches();
		void TryUpdateAllBranches();
	}
}