using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitStatusService
	{
		Task<R<GitStatus>> GetStatusAsync(CancellationToken ct);
		Task<R<GitConflicts>> GetConflictsAsync(CancellationToken ct);
		Task<R<string>> GetConflictFile(string fileId, CancellationToken ct);

		Task<R<IReadOnlyList<string>>> UndoAllUncommittedAsync(CancellationToken ct);
		Task<R<IReadOnlyList<string>>> CleanWorkingFolderAsync(CancellationToken ct);


		Task<R> UndoUncommittedFileAsync(
			string path, CancellationToken ct);


		Task<R<IReadOnlyList<string>>> GetRefsIdsAsync(CancellationToken ct);
		Task<R> AddAsync(string path, CancellationToken ct);
		Task<R> RemoveAsync(string path, CancellationToken ct);
	}
}