using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.Features.Diffing
{
	internal interface IGitDiffService
	{
		Task<R<CommitDiff>> GetCommitDiffAsync(string commitId);

		Task<R<CommitDiff>> GetCommitDiffRangeAsync(string id1, string id2);

		Task<R<CommitDiff>> GetFileDiffAsync(string commitId, string path);

		void GetFile(string fileId, string filePath);

		Task ResolveAsync(string path);
	}
}