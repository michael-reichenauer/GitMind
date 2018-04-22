//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using GitMind.ApplicationHandling;
//using GitMind.Common;
//using GitMind.Features.StatusHandling;
//using GitMind.GitModel;
//using GitMind.GitModel.Private;
//using GitMind.Utils;
//using GitMind.Utils.Git;
//using LibGit2Sharp;


//namespace GitMind.Git.Private
//{
//	internal class GitCommitsService : IGitCommitsService
//	{
//		//private static readonly StatusOptions StatusOptions = new StatusOptions
//		//{ DetectRenamesInWorkDir = true, DetectRenamesInIndex = true };

//		private readonly WorkingFolder workingFolder;
//		private readonly IGitCommitBranchNameService gitCommitBranchNameService;
//		private readonly IStatusService statusService;
//		private readonly IGitCommitService2 gitCommitService2;
//		private readonly IGitStatusService2 gitStatusService2;
//		private readonly IRepoCaller repoCaller;



//		public GitCommitsService(
//			WorkingFolder workingFolder,
//			IGitCommitBranchNameService gitCommitBranchNameService,
//			IStatusService statusService,
//			IGitCommitService2 gitCommitService2,
//			IGitStatusService2 gitStatusService2,
//			IRepoCaller repoCaller)
//		{
//			this.workingFolder = workingFolder;
//			this.gitCommitBranchNameService = gitCommitBranchNameService;
//			this.statusService = statusService;
//			this.gitCommitService2 = gitCommitService2;
//			this.gitStatusService2 = gitStatusService2;
//			this.repoCaller = repoCaller;
//		}


//		//public async Task<R<IReadOnlyList<GitFile2>>> GetFilesForCommitAsync(CommitSha commitSha)
//		//{
//		//	if (commitSha == CommitSha.Uncommitted)
//		//	{
//		//		R<GitStatus2> status = await gitStatusService2.GetStatusAsync(CancellationToken.None);
//		//		if (status.IsOk)
//		//		{
//		//			return R.From(status.Value.Files);
//		//		}
//		//		else
//		//		{
//		//			return status.Error;
//		//		}
//		//	}

//		//	return await gitCommitService2.GetCommitFilesAsync(commitSha.Sha, CancellationToken.None);
//		//}


//		public Task EditCommitBranchAsync(CommitSha commitSha, CommitSha rootSha, BranchName branchName) =>
//			gitCommitBranchNameService.EditCommitBranchNameAsync(commitSha, rootSha, branchName);


//		public IReadOnlyList<CommitBranchName> GetSpecifiedNames(CommitSha rootSha) =>
//			gitCommitBranchNameService.GetEditedBranchNames(rootSha);


//		public IReadOnlyList<CommitBranchName> GetCommitBranches(CommitSha rootSha) =>
//			gitCommitBranchNameService.GetCommitBrancheNames(rootSha);


//		public async Task<R> ResetMerge() => await gitStatusService2.UndoAllUncommitedAsync(CancellationToken.None);


//		public Task<R> UnCommitAsync() => gitCommitService2.UnCommitAsync(CancellationToken.None);


//		public Task<R> UndoCommitAsync(CommitSha commitSha) =>
//			gitCommitService2.UndoCommitAsync(commitSha.Sha, CancellationToken.None);


//		public Task<R<IReadOnlyList<string>>> CleanWorkingFolderAsync() =>
//			gitStatusService2.CleanWorkingFolderAsync(CancellationToken.None);


//		public async Task UndoWorkingFolderAsync() =>
//			await gitStatusService2.UndoAllUncommitedAsync(CancellationToken.None);


//		public async Task<R<GitCommit>> CommitAsync(
//			string message, string branchName, IReadOnlyList<CommitFile> paths)
//		{
//			Log.Debug($"Commit {paths.Count} files: {message} ...");

//			R<GitCommit> commit = await gitCommitService2.CommitAllChangesAsync(message, CancellationToken.None);
//			if (commit.IsOk)
//			{
//				CommitSha commitSha = commit.Value.Sha;
//				await gitCommitBranchNameService.SetCommitBranchNameAsync(commitSha, branchName);
//			}

//			return commit;
//		}


//		public async Task UndoFileInWorkingFolderAsync(string path) => 
//			await gitStatusService2.UndoUncommitedFileAsync(path, CancellationToken.None);

//	}
//}