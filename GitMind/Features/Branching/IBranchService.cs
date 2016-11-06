using System.Threading.Tasks;
using GitMind.GitModel;


namespace GitMind.Features.Branching
{
	internal interface IBranchService
	{
		Task CreateBranchAsync(Branch branch);

		Task CreateBranchFromCommitAsync(Commit commit);
		Task SwitchBranchAsync(Branch branch);
		bool CanExecuteSwitchBranch(Branch branch);
		Task SwitchToBranchCommitAsync(Commit commit);
		bool CanExecuteSwitchToBranchCommit(Commit commit);
		void DeleteBranch(Branch branch);
		Task MergeBranchAsync(Branch branch);
		void PublishBranch(Branch branch);
		void PushBranch(Branch branch);
		void UpdateBranch(Branch branch);
	}
}