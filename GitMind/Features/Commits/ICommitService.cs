using System.Threading.Tasks;
using GitMind.GitModel;


namespace GitMind.Features.Commits
{
	internal interface ICommitService
	{
		Task ShowUncommittedDiff();
		Task UndoUncommittedFileAsync(string path);
		Task CommitChangesAsync();
		Task UnCommitAsync(Commit commit);
		bool CanUnCommit(Commit commit);

		Task EditCommitBranchAsync(Commit commit);

		Task UndoUncommittedChangesAsync();
	}
}