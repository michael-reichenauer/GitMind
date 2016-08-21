using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.RepositoryViews;


namespace GitMind.Features.Branching
{
	internal interface ICreateBranchService
	{
		Task CreateBranchAsync(IRepositoryCommands repositoryCommands, Branch branch);

		Task CreateBranchFromCommitAsync(IRepositoryCommands repositoryCommands, Commit commit);
	}
}