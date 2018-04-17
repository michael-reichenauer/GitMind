//using System.Threading.Tasks;
//using GitMind.Common;
//using GitMind.Git;
//using GitMind.Utils;


//namespace GitMind.Features.Diffing.Private
//{
//	internal interface IGitDiffService
//	{
//		Task<R<CommitDiff>> GetCommitDiffAsync(CommitSha commitSha);

//		Task<R<CommitDiff>> GetCommitDiffRangeAsync(CommitSha commitSha1, CommitSha commitSha2);

//		Task<R<CommitDiff>> GetPreviewMergeDiffAsync(CommitSha commitSha1, CommitSha commitSha2);

//		Task<R<CommitDiff>> GetFileDiffAsync(CommitSha commitSha, string path);

//		void GetFile(string fileId, string filePath);

//		Task ResolveAsync(string path);
//	}
//}