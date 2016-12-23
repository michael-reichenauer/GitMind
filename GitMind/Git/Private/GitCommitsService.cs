using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Common;
using GitMind.Features.StatusHandling;
using GitMind.GitModel;
using GitMind.GitModel.Private;
using GitMind.RepositoryViews;
using GitMind.Utils;
using LibGit2Sharp;


namespace GitMind.Git.Private
{
	internal class GitCommitsService : IGitCommitsService
	{
		private static readonly StatusOptions StatusOptions = new StatusOptions
		{ DetectRenamesInWorkDir = true, DetectRenamesInIndex = true };

		private readonly WorkingFolder workingFolder;
		private readonly Lazy<IRepositoryMgr> repositoryMgr;
		private readonly IGitCommitBranchNameService gitCommitBranchNameService;
		private readonly IStatusService statusService;
		private readonly IRepoCaller repoCaller;



		public GitCommitsService(
			WorkingFolder workingFolder,
			Lazy<IRepositoryMgr> repositoryMgr,
			IGitCommitBranchNameService gitCommitBranchNameService,
			IStatusService statusService,
			IRepoCaller repoCaller)
		{
			this.workingFolder = workingFolder;
			this.repositoryMgr = repositoryMgr;
			this.gitCommitBranchNameService = gitCommitBranchNameService;
			this.statusService = statusService;
			this.repoCaller = repoCaller;
		}


		public async Task<R<IReadOnlyList<StatusFile>>> GetFilesForCommitAsync(CommitSha commitSha)
		{
			if (commitSha == CommitSha.Uncommitted)
			{
				Status status = repositoryMgr.Value.Repository.Status;
				return R.From(status.ChangedFiles);
			}

			return await repoCaller.UseRepoAsync(repo => repo.Diff.GetFiles(workingFolder, commitSha));
		}


		public Task EditCommitBranchAsync(CommitSha commitSha, CommitSha rootSha, BranchName branchName)
		{
			return gitCommitBranchNameService.EditCommitBranchNameAsync(commitSha, rootSha, branchName);
		}


		public IReadOnlyList<CommitBranchName> GetSpecifiedNames(CommitSha rootSha)
		{
			return gitCommitBranchNameService.GetEditedBranchNames(rootSha);
		}


		public IReadOnlyList<CommitBranchName> GetCommitBranches(CommitSha rootSha)
		{
			return gitCommitBranchNameService.GetCommitBrancheNames(rootSha);
		}


		public Task<R> ResetMerge()
		{
			return repoCaller.UseRepoAsync(repo => repo.Reset(ResetMode.Hard));
		}


		public Task<R> UnCommitAsync()
		{
			return repoCaller.UseRepoAsync(
				repo => repo.Reset(ResetMode.Mixed, repo.Head.Commits.ElementAt(1)));
		}


		public Task<R<IReadOnlyList<string>>> CleanWorkingFolderAsync()
		{
			return repoCaller.UseLibRepoAsync(repo =>
			{
				List<string> failedPaths = new List<string>();

				RepositoryStatus repositoryStatus = repo.RetrieveStatus(StatusOptions);
				foreach (StatusEntry statusEntry in repositoryStatus.Ignored.Concat(repositoryStatus.Untracked))
				{
					string path = statusEntry.FilePath;
					string fullPath = Path.Combine(workingFolder, path);
					try
					{
						if (File.Exists(fullPath))
						{
							Log.Debug($"Delete file {fullPath}");
							File.Delete(fullPath);
						}
						else if (Directory.Exists(fullPath))
						{
							Log.Debug($"Delete folder {fullPath}");
							Directory.Delete(fullPath, true);
						}
					}
					catch (Exception e)
					{
						Log.Warn($"Failed to delete {path}, {e.Message}");
						failedPaths.Add(fullPath);
					}
				}

				return failedPaths.AsReadOnlyList();
			});
		}


		public Task UndoWorkingFolderAsync()
		{
			return repoCaller.UseRepoAsync(repo =>
			{
				Log.Debug("Undo changes in working folder");
				repo.Reset(ResetMode.Hard);

				RepositoryStatus repositoryStatus = repo.RetrieveStatus(StatusOptions);
				foreach (StatusEntry statusEntry in repositoryStatus.Untracked)
				{
					string path = statusEntry.FilePath;
					try
					{
						string fullPath = Path.Combine(workingFolder, path);

						if (File.Exists(fullPath))
						{
							Log.Debug($"Delete file {fullPath}");
							File.Delete(fullPath);
						}
						else if (Directory.Exists(fullPath))
						{
							Log.Debug($"Delete folder {fullPath}");
							Directory.Delete(fullPath, true);
						}
					}
					catch (Exception e)
					{
						Log.Warn($"Failed to delete {path}, {e.Message}");
					}
				}
			});
		}


		public Task<R<GitCommit>> CommitAsync(
			string message, string branchName, IReadOnlyList<CommitFile> paths)
		{
			Log.Debug($"Commit {paths.Count} files: {message} ...");

			return repoCaller.UseLibRepoAsync(
				repo =>
				{
					AddPaths(repo, paths);
					GitCommit gitCommit = Commit(repo, message);
					CommitSha commitSha = gitCommit.Sha;
					gitCommitBranchNameService.SetCommitBranchNameAsync(commitSha, branchName);
					return gitCommit;
				});
		}


		public void AddPaths(LibGit2Sharp.Repository repo, IReadOnlyList<CommitFile> paths)
		{
			List<string> added = new List<string>();
			foreach (CommitFile commitFile in paths)
			{
				string fullPath = Path.Combine(workingFolder, commitFile.Path);
				if (File.Exists(fullPath))
				{
					repo.Index.Add(commitFile.Path);
					added.Add(commitFile.Path);
				}

				if (commitFile.OldPath != null && !added.Contains(commitFile.OldPath))
				{
					repo.Index.Remove(commitFile.OldPath);
				}

				if (commitFile.Status == GitFileStatus.Deleted)
				{
					repo.Remove(commitFile.Path);
				}
			}
		}


		public GitCommit Commit(LibGit2Sharp.Repository repo, string message)
		{
			Signature signature = repo.Config.BuildSignature(DateTimeOffset.Now);

			CommitOptions commitOptions = new CommitOptions();

			LibGit2Sharp.Commit commit = repo.Commit(message, signature, signature, commitOptions);

			return commit != null ? new GitCommit(
				new CommitSha(commit.Sha),
				commit.MessageShort,
				commit.Author.Name,
				commit.Author.When.LocalDateTime,
				commit.Committer.When.LocalDateTime,
				commit.Parents.Select(p => new CommitId(p.Sha)).ToList()) : null;
		}


		public async Task UndoFileInWorkingFolderAsync(string path)
		{
			Log.Debug($"Undo uncommitted file {path} ...");

			Status status = await statusService.GetStatusAsync();
			await repoCaller.UseLibRepoAsync(repo =>
			{
				StatusFile statusFile = status.ChangedFiles.FirstOrDefault(f => f.FilePath == path);

				if (statusFile != null)
				{
					if (statusFile.IsModified || statusFile.IsDeleted)
					{
						CheckoutOptions options = new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force };
						repo.CheckoutPaths("HEAD", new[] { path }, options);
					}

					if (statusFile.IsAdded || statusFile.IsRenamed)
					{
						string fullPath = Path.Combine(workingFolder, path);
						if (File.Exists(fullPath))
						{
							File.Delete(fullPath);
						}
					}

					if (statusFile.IsRenamed)
					{
						CheckoutOptions options = new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force };
						repo.CheckoutPaths("HEAD", new[] { statusFile.OldFilePath }, options);
					}
				}
			});
		}

		public R<string> GetFullMessage(CommitSha commitSha)
		{
			return repoCaller.UseRepo(repo =>
			{
				LibGit2Sharp.Commit commit = repo.Lookup<LibGit2Sharp.Commit>(new ObjectId(commitSha.Sha));
				if (commit != null)
				{
					return commit.Message;
				}

				return null;
			});
		}
	}
}