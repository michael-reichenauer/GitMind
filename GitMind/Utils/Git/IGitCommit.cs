using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitCommit
	{
		Task<R<IReadOnlyList<GitFile2>>> GetCommitFilesAsync(
			string commit, CancellationToken ct);


		Task<R<string>> CommitAllChangesAsync(string message, CancellationToken ct);
	}
}