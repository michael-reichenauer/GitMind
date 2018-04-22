//using System.Collections.Generic;
//using System.Threading.Tasks;
//using GitMind.Common;
//using GitMind.Features.StatusHandling;
//using GitMind.GitModel;
//using GitMind.GitModel.Private;
//using GitMind.Utils;
//using GitMind.Utils.Git;


//namespace GitMind.Git
//{
//	internal interface IGitCommitsService
//	{
//		//Task<R<IReadOnlyList<GitFile2>>> GetFilesForCommitAsync(CommitSha commitSha);

//		Task EditCommitBranchAsync(CommitSha commitSha, CommitSha rootSha, BranchName branchName);
	
//		IReadOnlyList<CommitBranchName> GetSpecifiedNames(CommitSha rootSha);
//		IReadOnlyList<CommitBranchName> GetCommitBranches(CommitSha rootSha);

	
//		Task<R<GitCommit>> CommitAsync(string message, string branchName, IReadOnlyList<CommitFile> paths);

//		//R<string> GetFullMessage(CommitSha commitSha);


//		Task<R<IReadOnlyList<string>>> CleanWorkingFolderAsync();

//		Task UndoFileInWorkingFolderAsync(string path);

//		Task UndoWorkingFolderAsync();
//		Task<R> ResetMerge();
//		Task<R> UnCommitAsync();
//		Task<R> UndoCommitAsync(CommitSha commitSha);
//	}
//}