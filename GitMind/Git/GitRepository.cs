using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitMind.GitModel;
using GitMind.Utils;
using LibGit2Sharp;
using Branch = LibGit2Sharp.Branch;
using Commit = LibGit2Sharp.Commit;
using Repository = LibGit2Sharp.Repository;


namespace GitMind.Git
{
	internal class GitRepository : IDisposable
	{
		// string emptyTreeSha = "4b825dc642cb6eb9a060e54bf8d69288fbee4904";;

		private readonly string workingFolder;
		private readonly Repository repository;
		private static readonly StatusOptions StatusOptions =
			new StatusOptions { DetectRenamesInWorkDir = true, DetectRenamesInIndex = true };
		private static readonly MergeOptions MergeFastForwardOnly =
			new MergeOptions { FastForwardStrategy = FastForwardStrategy.FastForwardOnly };
		private static readonly MergeOptions MergeDefault =
			new MergeOptions { FastForwardStrategy = FastForwardStrategy.Default };
		private static readonly MergeOptions MergeNoFastForward =
			new MergeOptions { FastForwardStrategy = FastForwardStrategy.NoFastForward, CommitOnSuccess = false };



		public GitRepository(string workingFolder, Repository repository)
		{
			this.workingFolder = workingFolder;
			this.repository = repository;
		}


		public IEnumerable<GitBranch> Branches => repository.Branches.Select(b => new GitBranch(b));

		public IEnumerable<GitTag> Tags => repository.Tags.Select(t => new GitTag(t));

		public GitBranch Head => new GitBranch(repository.Head);

		public GitStatus Status => GetGitStatus();


		public GitDiff Diff => new GitDiff(repository.Diff, repository);

		public string UserName => repository.Config.GetValueOrDefault<string>("user.name");


		public void Dispose()
		{
			repository.Dispose();
		}


		public void Fetch()
		{
			repository.Fetch("origin");
		}


		public GitCommit Commit(string message)
		{
			Signature author = repository.Config.BuildSignature(DateTimeOffset.Now);
			Signature committer = repository.Config.BuildSignature(DateTimeOffset.Now);
			CommitOptions commitOptions = new CommitOptions();

			Commit commit = repository.Commit(message, author, committer, commitOptions);

			return commit != null ? new GitCommit(commit) : null;
		}


		public void MergeCurrentBranchFastForwardOnly()
		{
			Signature committer = repository.Config.BuildSignature(DateTimeOffset.Now);
			repository.MergeFetchedRefs(committer, MergeFastForwardOnly);
		}


		public void MergeCurrentBranchNoFastForwardy()
		{
			Signature committer = repository.Config.BuildSignature(DateTimeOffset.Now);
			repository.MergeFetchedRefs(committer, MergeNoFastForward);
		}


		public void MergeCurrentBranch()
		{
			Signature committer = repository.Config.BuildSignature(DateTimeOffset.Now);
			repository.MergeFetchedRefs(committer, MergeDefault);
		}


		public void Add(IReadOnlyList<CommitFile> paths)
		{
			foreach (CommitFile commitFile in paths)
			{
				repository.Index.Add(commitFile.Path);
				if (commitFile.OldPath != null)
				{
					repository.Index.Remove(commitFile.OldPath);
				}

				if (commitFile.Status == GitFileStatus.Deleted)
				{
					repository.Remove(commitFile.Path);
				}
			}
		}


		private GitStatus GetGitStatus()
		{
			RepositoryStatus repositoryStatus = repository.RetrieveStatus(StatusOptions);
			ConflictCollection conflicts = repository.Index.Conflicts;
			bool isFullyMerged = repository.Index.IsFullyMerged;
			return new GitStatus(repositoryStatus, conflicts, repository.Info, isFullyMerged);
		}


		public void Checkout(string branchName)
		{
			Branch branch = repository.Branches.FirstOrDefault(b => b.FriendlyName == branchName);

			if (branch != null)
			{
				repository.Checkout(branch);
			}
			else
			{
				Branch remoteBranch = repository.Branches.FirstOrDefault(b => b.FriendlyName == "origin/" + branchName);
				if (remoteBranch != null)
				{
					branch = repository.Branches.Add(branchName, remoteBranch.Tip);
					repository.Branches.Update(branch, b => b.TrackedBranch = remoteBranch.CanonicalName);

					repository.Checkout(branch);
				}
			}
		}


		public void UndoFileInCurrentBranch(string path)
		{
			GitStatus gitStatus = GetGitStatus();

			GitFile gitFile = gitStatus.CommitFiles.Files.FirstOrDefault(f => f.File == path);

			if (gitFile != null)
			{
				if (gitFile.IsModified || gitFile.IsDeleted)
				{
					CheckoutOptions options = new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force };
					repository.CheckoutPaths("HEAD", new[] { path }, options);
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
					repository.CheckoutPaths("HEAD", new[] { gitFile.OldFile }, options);
				}
			}
		}


		public GitCommit MergeBranchNoFastForward(string branchName)
		{
			Signature committer = repository.Config.BuildSignature(DateTimeOffset.Now);

			Branch localbranch = repository.Branches.FirstOrDefault(b => b.FriendlyName == branchName);
			Branch remoteBranch = repository.Branches.FirstOrDefault(b => b.FriendlyName == "origin/" + branchName);

			Branch branch = localbranch ?? remoteBranch;
			if (localbranch != null && remoteBranch != null)
			{
				if (remoteBranch.Tip.Committer.When.LocalDateTime > localbranch.Tip.Committer.When.LocalDateTime)
				{
					branch = remoteBranch;
				}
			}

			if (branch != null)
			{
				MergeResult mergeResult = repository.Merge(branch, committer, MergeNoFastForward);
				if (mergeResult?.Commit != null)
				{
					return new GitCommit(mergeResult.Commit);
				}
				else
				{
					RepositoryStatus repositoryStatus = repository.RetrieveStatus(new StatusOptions());

					if (!repositoryStatus.IsDirty)
					{
						// Empty merge with no changes, lets reset merge since there is nothing to merge
						repository.Reset(ResetMode.Hard);
					}

					return null;
				}
			}

			return null;
		}


		public string SwitchToCommit(string commitId, string proposedBranchName)
		{
			Commit commit = repository.Lookup<Commit>(new ObjectId(commitId));
			if (commit == null)
			{
				Log.Warn($"Unknown commit id {commitId}");
				return null;
			}

			// Trying to create a switch branch and check out, but that branch might be "taken"
			// so we might have to retry a few times
			for (int i = 0; i < 10; i++)
			{
				// Trying to get an existing switch branch with proposed name) at that commit
				Branch branch = repository.Branches
					.FirstOrDefault(b => !b.IsRemote && b.FriendlyName == proposedBranchName && b.Tip.Id.Sha == commitId);

				if (branch == null)
				{
					// Could not find proposed name at that place, try get existing branch at that commit
					branch = repository.Branches.FirstOrDefault(b => !b.IsRemote && b.Tip.Id.Sha == commitId);
				}

				string branchName = (i == 0) ? proposedBranchName : $"{commit.Sha.Substring(0, 6)}_{i + 1}";

				if (branch == null)
				{
					// Try get a previous switch branch				
					branch = repository.Branches.FirstOrDefault(b => b.FriendlyName == branchName);
				}

				if (branch != null && branch.Tip.Id.Sha != commitId)
				{
					// Branch name already exist, but no longer point to specified commit, lets try other name
					continue;
				}
				else if (branch == null)
				{
					// No branch with that name so lets create one
					branch = repository.Branches.Add(branchName, commit);
				}

				repository.Checkout(branch);

				return branchName;
			}

			Log.Warn($"To many branches with same name");
			return null;
		}


		public void CreateBranch(string branchName, string commitId, bool isPublish)
		{
			Commit commit = repository.Lookup<Commit>(new ObjectId(commitId));
			if (commit == null)
			{
				Log.Warn($"Unknown commit id {commitId}");
				return;
			}

			Branch branch = repository.Branches.FirstOrDefault(b => b.FriendlyName == branchName);


			if (branch != null)
			{
				Log.Warn($"Branch already exists {branchName}");
				return;
			}

			branch = repository.Branches.Add(branchName, commit);
			repository.Checkout(branch);

			Branch remoteBranch = repository.Branches.FirstOrDefault(b => b.FriendlyName == "origin/" + branchName);

			if (isPublish && remoteBranch != null)
			{
				branch = repository.Branches.Add(branchName, remoteBranch.Tip);
				repository.Branches.Update(branch, b => b.TrackedBranch = remoteBranch.CanonicalName);
			}
		}


		public string GetFullMessage(string commitId)
		{
			Commit commit = repository.Lookup<Commit>(new ObjectId(commitId));
			if (commit != null)
			{
				return commit.Message;
			}

			return null;
		}


		public IReadOnlyList<GitNote> GetCommitNotes(string commitId)
		{
			Commit commit = repository.Lookup<Commit>(new ObjectId(commitId));
			if (commit != null)
			{
				return commit.Notes
					.Select(note => new GitNote(note.Namespace ?? "", note.Message))
					.ToList();
			}
			else
			{
				Log.Warn($"Could not find commit {commitId}");
			}

			return new GitNote[0];
		}


		public void SetCommitNote(string commitId, GitNote gitNote)
		{
			Signature committer = repository.Config.BuildSignature(DateTimeOffset.Now);

			Commit commit = repository.Lookup<Commit>(new ObjectId(commitId));
			if (commit != null)
			{
				repository.Notes.Add(commit.Id, gitNote.Message, committer, committer, gitNote.NameSpace);
			}
			else
			{
				Log.Warn($"Could not find commit {commitId}");
			}
		}


		public IReadOnlyList<string> UndoCleanWorkingFolder()
		{
			List<string> failedPaths = new List<string>();

			repository.Reset(ResetMode.Hard);

			RepositoryStatus repositoryStatus = repository.RetrieveStatus(StatusOptions);
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

			return failedPaths;
		}


		public void UndoWorkingFolder()
		{
			Log.Debug("Undo changes in working folder");
			repository.Reset(ResetMode.Hard);

			RepositoryStatus repositoryStatus = repository.RetrieveStatus(StatusOptions);
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
		}


		public void GetFile(string fileId, string filePath)
		{
			Blob blob = repository.Lookup<Blob>(new ObjectId(fileId));

			if (blob != null)
			{
				using (var stream = File.Create(filePath))
				{
					blob.GetContentStream().CopyTo(stream);
				}
			}
		}

		public void Resolve(string path)
		{
			string fullPath = Path.Combine(workingFolder, path);
			Log.Debug($"Resolving {path}");
			if (File.Exists(fullPath))
			{
				repository.Index.Add(path);
			}
			else
			{
				repository.Remove(path);
			}

			// Temp workaround to trigger status update after resolving conflicts, ill be handled better
			string tempPath = fullPath + ".tmp";
			File.AppendAllText(tempPath, "tmp");
			File.Delete(tempPath);
		}
	}
}