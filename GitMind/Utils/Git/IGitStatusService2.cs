using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitStatusService2
	{
		Task<R<GitStatus2>> GetStatusAsync(CancellationToken ct);
		Task<R<GitConflicts>> GetConflictsAsync(CancellationToken ct);
		Task<R<string>> GetConflictFile(string fileId, CancellationToken ct);
	}
}