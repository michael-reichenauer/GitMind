using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.Features.Diffing
{
	internal interface IGitDiffService
	{
		Task<R<CommitDiff>> GetCommitDiffAsync(string workingFolder, string commitId);

		Task<R<CommitDiff>> GetCommitDiffRangeAsync(string workingFolder, string id1, string id2);

		Task<R<CommitDiff>> GetFileDiffAsync(string workingFolder, string commitId, string path);

		void GetFile(string workingFolder, string fileId, string filePath);

		Task ResolveAsync(string workingFolder, string path);
	}
}