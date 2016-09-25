﻿using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal class GitService : IGitService
	{
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


		public Task UndoFileInCurrentBranchAsync(string workingFolder, string path)
		{
			Log.Debug($"Undo uncommitted file {path} ...");
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.UndoFileInCurrentBranch(path));
		}


		public R<string> GetFullMessage(string workingFolder, string commitId)
		{
			Log.Debug($"Get full commit message for commit {commitId} ...");
			return repoCaller.UseRepo(workingFolder, repo => repo.GetFullMessage(commitId));
		}
	}
}