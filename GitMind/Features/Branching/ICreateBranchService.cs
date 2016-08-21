using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.RepositoryViews;


namespace GitMind.Features.Branching
{
	internal interface ICreateBranchService
	{
		Task CreateBranchAsync(RepositoryViewModel viewModel, Branch branch);
		Task CreateBranchFromCommitAsync(RepositoryViewModel viewModel, Commit commit);
	}
}