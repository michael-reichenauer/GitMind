using System.Threading.Tasks;
using GitMind.GitModel;


namespace GitMind.Features.Commits
{
	internal interface ICommitsService
	{
		Task ShowUncommittedDiffAsync();
		Task UndoUncommittedFileAsync(string path);
		Task CommitChangesAsync(string mergeCommitMessage = null);
		Task UnCommitAsync(Commit commit);
		bool CanUnCommit(Commit commit);
		bool CanUndoCommit(Commit commit);

		Task EditCommitBranchAsync(Commit commit);

		Task UndoUncommittedChangesAsync();

		Task CleanWorkingFolderAsync();
		Task UndoCommitAsync(Commit commit);

		Links GetIssueLinks(string text);
		Links GetTagLinks(string text);
	}
}