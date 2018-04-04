using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitDiff
	{
		Task<R<IReadOnlyList<GitFile2>>> GetFilesAsync(string sha, CancellationToken ct);
	}
}