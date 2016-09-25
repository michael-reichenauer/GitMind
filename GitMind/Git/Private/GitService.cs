﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.Utils;
using LibGit2Sharp;


namespace GitMind.Git.Private
{
	internal class GitService : IGitService
	{
		private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(30);
		private static readonly TimeSpan PushTimeout = TimeSpan.FromSeconds(30);


		private readonly IGitDiffParser gitDiffParser;
		private readonly IGitCommitBranchNameService gitCommitBranchNameService;
		private readonly IRepoCaller repoCaller;


		public GitService(
			IGitDiffParser gitDiffParser,
			IGitCommitBranchNameService gitCommitBranchNameService,
			IRepoCaller repoCaller)
		{
			this.gitDiffParser = gitDiffParser;
			this.gitCommitBranchNameService = gitCommitBranchNameService;
			this.repoCaller = repoCaller;
		}


		public GitService()
			: this(new GitDiffParser(), new GitCommitBranchNameService(), new RepoCaller())
		{
		}



		public Task<R<GitStatus>> GetStatusAsync(string workingFolder)
		{
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.Status);
		}


		public Task<R<GitCommitFiles>> GetFilesForCommitAsync(string workingFolder, string commitId)
		{
			Log.Debug($"Getting files for {commitId} ...");
			return repoCaller.UseRepoAsync(workingFolder, repo =>
			{
				if (commitId == GitCommit.UncommittedId)
				{
					return repo.Status.CommitFiles;
				}

				return repo.Diff.GetFiles(commitId);
			});
		}


		public Task EditCommitBranchAsync(
			string workingFolder, 
			string commitId,
			string rootId,
			BranchName branchName, 
			ICredentialHandler credentialHandler)
		{
			return gitCommitBranchNameService.EditCommitBranchNameAsync(
				workingFolder, commitId, rootId, branchName, credentialHandler);
		}


		//public Task SetCommitBranchNameAsync(string workingFolder, string commitId, BranchName branchName)
		//{
		//	return GitCommitBranchNameService.SetCommitBranchNameAsync(workingFolder, commitId, branchName);
		//}


		public IReadOnlyList<CommitBranchName> GetSpecifiedNames(string workingFolder, string rootId)
		{
			return gitCommitBranchNameService.GetEditedBranchNames(workingFolder, rootId);
		}


		public IReadOnlyList<CommitBranchName> GetCommitBranches(string workingFolder, string rootId)
		{
			return gitCommitBranchNameService.GetCommitBrancheNames(workingFolder, rootId);
		}


		public Task<R<CommitDiff>> GetFileDiffAsync(string workingFolder, string commitId, string path)
		{
			Log.Debug($"Get diff for file {path} for commit {commitId} ...");
			return repoCaller.UseRepoAsync(workingFolder, async repo =>
			{
				string patch = repo.Diff.GetFilePatch(commitId, path);

				return await gitDiffParser.ParseAsync(commitId, patch, false);
			});
		}


		public Task<R<CommitDiff>> GetCommitDiffAsync(string workingFolder, string commitId)
		{
			Log.Debug($"Get diff for commit {commitId} ...");
			return repoCaller.UseRepoAsync(workingFolder, async repo =>
			{
				string patch = repo.Diff.GetPatch(commitId);

				return await gitDiffParser.ParseAsync(commitId, patch);
			});
		}


		public Task<R<CommitDiff>> GetCommitDiffRangeAsync(string workingFolder, string id1, string id2)
		{
			Log.Debug($"Get diff for commit range {id1}-{id2} ...");
			return repoCaller.UseRepoAsync(workingFolder, async repo =>
			{
				string patch = repo.Diff.GetPatchRange(id1, id2);

				return await gitDiffParser.ParseAsync(null, patch);
			});
		}


		public async Task FetchAsync(string workingFolder)
		{
			await repoCaller.UseRepoAsync(workingFolder, FetchTimeout, repo => repo.Fetch());
		}


		public Task FetchBranchAsync(string workingFolder, BranchName branchName)
		{
			Log.Debug($"Fetch branch {branchName}...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.FetchBranch(branchName));
		}


		public Task FetchAllNotesAsync(string workingFolder)
		{
			return gitCommitBranchNameService.FetchAllNotesAsync(workingFolder);
		}


		public Task<R<IReadOnlyList<string>>> UndoCleanWorkingFolderAsync(string workingFolder)
		{
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.UndoCleanWorkingFolder());
		}


		public Task UndoWorkingFolderAsync(string workingFolder)
		{
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.UndoWorkingFolder());
		}


		public void GetFile(string workingFolder, string fileId, string filePath)
		{
			Log.Debug($"Get file {fileId}, {filePath} ...");
			repoCaller.UseRepo(workingFolder, repo => repo.GetFile(fileId, filePath));
		}


		public Task ResolveAsync(string workingFolder, string path)
		{
			Log.Debug($"Resolve {path}  ...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.Resolve(path));
		}


		public Task<R> DeleteBranchAsync(string workingFolder, BranchName branchName, bool isRemote, ICredentialHandler credentialHandler)
		{
			if (isRemote)
			{
				return DeleteRemoteBranchAsync(workingFolder, branchName, credentialHandler);
			}
			else
			{
				return DeleteLocalBranchAsync(workingFolder, branchName);
			}
		}


		private Task<R> DeleteLocalBranchAsync(string workingFolder, BranchName branchName)
		{
			Log.Debug($"Delete local branch {branchName}  ...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.DeleteLocalBranch(branchName));
		}


		private Task<R> DeleteRemoteBranchAsync(
			string workingFolder, BranchName branchName, ICredentialHandler credentialHandler)
		{
			Log.Debug($"Delete remote branch {branchName} ...");
			return repoCaller.UseRepoAsync(workingFolder, PushTimeout, repo =>
				repo.DeleteRemoteBranch(branchName, credentialHandler));
		}


		public Task MergeCurrentBranchFastForwardOnlyAsync(string workingFolder)
		{
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.MergeCurrentBranchFastForwardOnly());
		}


		public Task MergeCurrentBranchAsync(string workingFolder)
		{
			return repoCaller.UseRepoAsync(workingFolder, repo =>
			{
				// First try to update using fast forward merge only
				R result = repo.MergeCurrentBranchFastForwardOnly();

				if (result.Error.Is<NonFastForwardException>())
				{
					// Failed with fast forward merge, trying no fast forward.
					repo.MergeCurrentBranchNoFastForward();
				}
			});
		}


		public Task PushCurrentBranchAsync(
			string workingFolder, ICredentialHandler credentialHandler)
		{
			return repoCaller.UseRepoAsync(workingFolder, PushTimeout,
				repo => repo.PushCurrentBranch(credentialHandler));
		}


		public Task PushNotesAsync(
			string workingFolder, string rootId, ICredentialHandler credentialHandler)
		{
			return gitCommitBranchNameService.PushNotesAsync(workingFolder, rootId, credentialHandler);
		}


		public Task PushBranchAsync(string workingFolder, BranchName branchName, ICredentialHandler credentialHandler)
		{
			Log.Debug($"Push branch {branchName} ...");
			return repoCaller.UseRepoAsync(workingFolder, PushTimeout,
				repo => repo.PushBranch(branchName, credentialHandler));
		}


		public Task<R<GitCommit>> CommitAsync(
			string workingFolder, string message, string branchName, IReadOnlyList<CommitFile> paths)
		{
			Log.Debug($"Commit {paths.Count} files: {message} ...");
			
			return repoCaller.UseRepoAsync(workingFolder,
				repo =>
				{
					repo.Add(paths);
					GitCommit gitCommit = repo.Commit(message);
					gitCommitBranchNameService.SetCommitBranchNameAsync(workingFolder, gitCommit.Id, branchName);
					return gitCommit;
				});

		
		}


		public Task SwitchToBranchAsync(string workingFolder, BranchName branchName)
		{
			Log.Debug($"Switch to branch {branchName} ...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.Checkout(branchName));
		}


		public Task<R<BranchName>> SwitchToCommitAsync(
			string workingFolder, string commitId, BranchName branchName)
		{
			Log.Debug($"Switch to commit {commitId} with branch name '{branchName}' ...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.SwitchToCommit(commitId, branchName));
		}


		public Task UndoFileInCurrentBranchAsync(string workingFolder, string path)
		{
			Log.Debug($"Undo uncommitted file {path} ...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.UndoFileInCurrentBranch(path));
		}


		public Task<R<GitCommit>> MergeAsync(string workingFolder, BranchName branchName)
		{
			Log.Debug($"Merge branch {branchName} into current branch ...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.MergeBranchNoFastForward(branchName));
		}


		public Task CreateBranchAsync(string workingFolder, BranchName branchName, string commitId)
		{
			Log.Debug($"Create branch {branchName} at commit {commitId} ...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.CreateBranch(branchName, commitId));
		}


		public Task<R> PublishBranchAsync(string workingFolder, BranchName branchName, ICredentialHandler credentialHandler)
		{
			Log.Debug($"Publish branch {branchName} ...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.PublishBranch(branchName, credentialHandler));
		}


		public bool IsSupportedRemoteUrl(string workingFolder)
		{
			return repoCaller.UseRepo(workingFolder, repo => repo.IsSupportedRemoteUrl()).Or(false);
		}


		public R<string> GetFullMessage(string workingFolder, string commitId)
		{
			Log.Debug($"Get full commit message for commit {commitId} ...");
			return repoCaller.UseRepo(workingFolder, repo => repo.GetFullMessage(commitId));
		}
	}
}