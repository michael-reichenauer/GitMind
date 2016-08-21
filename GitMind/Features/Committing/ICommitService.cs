using System.Threading.Tasks;
using GitMind.RepositoryViews;


namespace GitMind.Features.Committing
{
	internal interface ICommitService
	{
		Task ShowUncommittedDiff(IRepositoryCommands repositoryCommands);
		Task UndoUncommittedFileAsync(IRepositoryCommands repositoryCommands, string path);
		Task CommitChangesAsync(IRepositoryCommands repositoryCommands);
	}
}