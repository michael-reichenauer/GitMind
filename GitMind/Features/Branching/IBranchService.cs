using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.RepositoryViews;


namespace GitMind.Features.Branching
{
	internal interface IBranchService
	{
		Task CreateBranchAsync(IRepositoryCommands repositoryCommands, Branch branch);

		Task CreateBranchFromCommitAsync(IRepositoryCommands repositoryCommands, Commit commit);
		Task SwitchBranchAsync(IRepositoryCommands repositoryCommands, Branch branch);
		bool CanExecuteSwitchBranch(Branch branch);
		Task SwitchToBranchCommitAsync(IRepositoryCommands repositoryCommands, Commit commit);
		bool CanExecuteSwitchToBranchCommit(Commit commit);
		void DeleteLocalBranch(IRepositoryCommands repositoryCommands, Branch branch);
		void DeleteRemoteBranch(IRepositoryCommands repositoryCommands, Branch branch);
		Task MergeBranchAsync(IRepositoryCommands repositoryCommands, Branch branch);
	}
}