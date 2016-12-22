using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.Features.Diffing.Private
{
	internal interface IGitDiffService
	{
		Task<R<CommitDiff>> GetCommitDiffAsync(CommitSha commitId);

		Task<R<CommitDiff>> GetCommitDiffRangeAsync(CommitSha id1, CommitSha id2);

		Task<R<CommitDiff>> GetFileDiffAsync(CommitSha commitId, string path);

		void GetFile(string fileId, string filePath);

		Task ResolveAsync(string path);
	}
}