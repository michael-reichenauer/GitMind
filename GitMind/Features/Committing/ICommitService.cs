using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.RepositoryViews;


namespace GitMind.Features.Committing
{
	internal interface ICommitService
	{
		Task ShowUncommittedDiff();
		Task UndoUncommittedFileAsync(string path);
		Task CommitChangesAsync();
		Task UnCommitAsync(Commit commit);
	}
}