using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.Git.Private;


namespace GitMind.Utils.Git
{
	public interface IGitCommit
	{
		Task<R<IReadOnlyList<CommitFile>>> GetCommitFilesAsync(
			string commit, CancellationToken ct);
	}
}