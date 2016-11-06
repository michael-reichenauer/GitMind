using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Features.Diffing;
using GitMind.GitModel;
using GitMind.Utils;
using LibGit2Sharp;



namespace GitMind.Git.Private
{
	internal class GitCommitsService : IGitCommitsService
	{
		private static readonly StatusOptions StatusOptions = new StatusOptions
		{ DetectRenamesInWorkDir = true, DetectRenamesInIndex = true };

		private readonly WorkingFolder workingFolder;
		private readonly IGitCommitBranchNameService gitCommitBranchNameService;
		private readonly IRepoCaller repoCaller;
		private readonly IDiffService diffService;


		public GitCommitsService(
			WorkingFolder workingFolder,
			IGitCommitBranchNameService gitCommitBranchNameService,
			IRepoCaller repoCaller,
			IDiffService diffService)
		{
			this.workingFolder = workingFolder;
			this.gitCommitBranchNameService = gitCommitBranchNameService;
			this.repoCaller = repoCaller;
			this.diffService = diffService;
		}


		public Task<R<GitCommitFiles>> GetFilesForCommitAsync(string commitId)
		{
			return repoCaller.UseRepoAsync(repo =>
			{
				if (commitId == GitCommit.UncommittedId)
				{
					return repo.Status.CommitFiles;
				}

				return repo.Diff.GetFiles(commitId);
			});
		}


		public Task EditCommitBranchAsync(string commitId, string rootId, BranchName branchName)
		{
			return gitCommitBranchNameService.EditCommitBranchNameAsync(commitId, rootId, branchName);
		}


		public IReadOnlyList<CommitBranchName> GetSpecifiedNames(string rootId)
		{
			return gitCommitBranchNameService.GetEditedBranchNames(rootId);
		}


		public IReadOnlyList<CommitBranchName> GetCommitBranches(string rootId)
		{
			return gitCommitBranchNameService.GetCommitBrancheNames(rootId);
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


		public Task<R<IReadOnlyList<string>>> UndoCleanWorkingFolderAsync()
		{
			return repoCaller.UseLibRepoAsync(repo =>
			{
				List<string> failedPaths = new List<string>();

				repo.Reset(ResetMode.Hard);

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
					gitCommitBranchNameService.SetCommitBranchNameAsync(gitCommit.Id, branchName);
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

			return commit != null ? new GitCommit(commit) : null;
		}


		public Task UndoFileInWorkingFolderAsync(string path)
		{
			Log.Debug($"Undo uncommitted file {path} ...");

			return repoCaller.UseLibRepoAsync(repo =>
			{
				GitStatus gitStatus = GetGitStatus(repo);

				GitFile gitFile = gitStatus.CommitFiles.Files.FirstOrDefault(f => f.File == path);

				if (gitFile != null)
				{
					if (gitFile.IsModified || gitFile.IsDeleted)
					{
						CheckoutOptions options = new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force };
						repo.CheckoutPaths("HEAD", new[] { path }, options);
					}

					if (gitFile.IsAdded || gitFile.IsRenamed)
					{
						string fullPath = Path.Combine(workingFolder, path);
						if (File.Exists(fullPath))
						{
							File.Delete(fullPath);
						}
					}

					if (gitFile.IsRenamed)
					{
						CheckoutOptions options = new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force };
						repo.CheckoutPaths("HEAD", new[] { gitFile.OldFile }, options);
					}
				}
			});
		}


		private GitStatus GetGitStatus(LibGit2Sharp.Repository repo)
		{
			RepositoryStatus repositoryStatus = repo.RetrieveStatus(StatusOptions);
			ConflictCollection conflicts = repo.Index.Conflicts;
			bool isFullyMerged = repo.Index.IsFullyMerged;

			return new GitStatus(diffService, repositoryStatus, conflicts, repo.Info, isFullyMerged);
		}


		public R<string> GetFullMessage(string commitId)
		{
			return repoCaller.UseRepo(repo =>
			{
				LibGit2Sharp.Commit commit = repo.Lookup<LibGit2Sharp.Commit>(new ObjectId(commitId));
				if (commit != null)
				{
					return commit.Message;
				}

				return null;
			});
		}
	}
}