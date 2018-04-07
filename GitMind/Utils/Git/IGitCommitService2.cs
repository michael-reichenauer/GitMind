using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.GitModel.Private;


namespace GitMind.Utils.Git
{
	public interface IGitCommitService2
	{
		Task<R<IReadOnlyList<GitFile2>>> GetCommitFilesAsync(
			string sha, CancellationToken ct);


		Task<R<GitCommit>> CommitAllChangesAsync(string message, CancellationToken ct);
		Task<R<GitCommit>> GetCommitAsync(string sha, CancellationToken ct);
		Task<R<IReadOnlyList<string>>> UndoUncommitedAsync(CancellationToken ct);
		Task<R<IReadOnlyList<string>>> CleanWorkingFolderAsync(CancellationToken ct);
		Task<R> UndoCommitAsync(string sha, CancellationToken ct);
		Task<R> UnCommitAsync(CancellationToken ct);
	}
}