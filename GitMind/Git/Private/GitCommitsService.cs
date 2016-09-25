using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal class GitCommitsService : IGitCommitsService
	{
		private readonly IGitCommitBranchNameService gitCommitBranchNameService;
		private readonly IRepoCaller repoCaller;


		public GitCommitsService(
			IGitCommitBranchNameService gitCommitBranchNameService,
			IRepoCaller repoCaller)
		{
			this.gitCommitBranchNameService = gitCommitBranchNameService;
			this.repoCaller = repoCaller;
		}


		public GitCommitsService()
			: this(new GitCommitBranchNameService(), new RepoCaller())
		{
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




		public Task<R<IReadOnlyList<string>>> UndoCleanWorkingFolderAsync(string workingFolder)
		{
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.UndoCleanWorkingFolder());
		}


		public Task UndoWorkingFolderAsync(string workingFolder)
		{
			return repoCaller.UseRepoAsync(workingFolder, repo => repo.UndoWorkingFolder());
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


		public Task UndoFileInWorkingFolderAsync(string workingFolder, string path)
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