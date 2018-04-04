using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.GitModel.Private;


namespace GitMind.Utils.Git
{
	public interface IGitCommit
	{
		Task<R<IReadOnlyList<GitFile2>>> GetCommitFilesAsync(
			string sha, CancellationToken ct);


		Task<R<GitCommit>> CommitAllChangesAsync(string message, CancellationToken ct);
		Task<R<GitCommit>> GetCommitAsync(string sha, CancellationToken ct);
	}
}