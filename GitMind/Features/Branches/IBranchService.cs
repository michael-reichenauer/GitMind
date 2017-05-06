using System.Threading.Tasks;
using GitMind.GitModel;


namespace GitMind.Features.Branches
{
	internal interface IBranchService
	{
		Task CreateBranchAsync(Branch branch);

		Task CreateBranchFromCommitAsync(Commit commit);
		Task SwitchBranchAsync(Branch branch);
		bool CanExecuteSwitchBranch(Branch branch);
		Task SwitchToBranchCommitAsync(Commit commit);
		bool CanExecuteSwitchToBranchCommit(Commit commit);
		Task DeleteBranchAsync(Branch branch);
		bool CanDeleteBranch(Branch branch);
		Task MergeBranchAsync(Branch branch);
		Task PublishBranchAsync(Branch branch);
		Task PushBranchAsync(Branch branch);
		Task UpdateBranchAsync(Branch branch);
		Task MergeBranchCommitAsync(Commit commit);
	}
}