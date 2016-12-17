using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.Features.Diffing.Private
{
	internal interface IGitDiffService
	{
		Task<R<CommitDiff>> GetCommitDiffAsync(CommitId commitId);

		Task<R<CommitDiff>> GetCommitDiffRangeAsync(CommitId id1, CommitId id2);

		Task<R<CommitDiff>> GetFileDiffAsync(CommitId commitId, string path);

		void GetFile(string fileId, string filePath);

		Task ResolveAsync(string path);
	}
}