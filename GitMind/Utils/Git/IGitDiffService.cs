using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitDiffService
	{
		Task<R<IReadOnlyList<GitFile2>>> GetFilesAsync(string sha, CancellationToken ct);
		Task<R<string>> GetCommitDiffAsync(string sha, CancellationToken ct);
		//Task<R<string>> GetCommitDiffAsync(string sha, string parentSha, CancellationToken ct);
		Task<R<string>> GetCommitDiffRangeAsync(string sha1, string sha2, CancellationToken ct);
		Task<R<string>> GetFileDiffAsync(string sha, string path, CancellationToken ct);
		Task<R<string>> GetPreviewMergeDiffAsync(string sha1, string sha2, CancellationToken ct);
		Task<R<string>> GetUncommittedDiffAsync(CancellationToken ct);
		Task<R<string>> GetUncommittedFileDiffAsync(string path, CancellationToken ct);
	}
}