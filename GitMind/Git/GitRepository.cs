using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using GitMind.Utils;
using LibGit2Sharp;



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

		public static GitRepository Open(string folder)
		{
			return new GitRepository(folder, new Repository(folder));
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
			FetchOptions options = new FetchOptions { Prune = true };
			repository.Fetch("origin", options);
		}


		public void FetchBranch(string branchName)
		{
			Remote remote = repository.Network.Remotes["origin"];

			repository.Network.Fetch(remote, new []{ $"{branchName}:{branchName}" });
		}


		public void FetchRefs(string refs)
		{
			Remote remote = repository.Network.Remotes["origin"];

			repository.Network.Fetch(remote, new[] { refs });
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


		public void Add(IReadOnlyList<GitModel.CommitFile> paths)
		{
			foreach (GitModel.CommitFile commitFile in paths)
			{
				string fullPath = Path.Combine(workingFolder, commitFile.Path);
				if (File.Exists(fullPath))
				{
					repository.Index.Add(commitFile.Path);
				}

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


		public void CreateBranch(string branchName, string commitId)
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
		}


		public void PublishBranch(string branchName, ICredentialHandler credentialHandler)
		{
			Branch localBranch = repository.Branches.FirstOrDefault(b => b.FriendlyName == branchName);
			if (localBranch == null)
			{
				Log.Warn($"Local branch does not exists {branchName}");
				return;
			}

			PushOptions pushOptions = GetPushOptions(credentialHandler);

			// Check if corresponding remote branch exists
			Branch remoteBranch = repository.Branches
				.FirstOrDefault(b => b.FriendlyName == "origin/" + branchName);			

			if (remoteBranch != null)
			{
				// Remote branch exists, so connect local and remote branch
				localBranch = repository.Branches.Add(branchName, remoteBranch.Tip);
				repository.Branches.Update(localBranch, b => b.TrackedBranch = remoteBranch.CanonicalName);
			}
			else
			{
				// Remote branch does not yet exists
				Remote remote = repository.Network.Remotes["origin"];

				repository.Branches.Update(
					localBranch,
					b => b.Remote = remote.Name,
					b => b.UpstreamBranch = localBranch.CanonicalName);
			}

			repository.Network.Push(localBranch, pushOptions);
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


		public void PushCurrentBranch(ICredentialHandler credentialHandler)
		{
			try
			{
				Branch currentBranch = repository.Head;

				PushOptions pushOptions = GetPushOptions(credentialHandler);

				repository.Network.Push(currentBranch, pushOptions);

				credentialHandler.SetConfirm(true);
			}
			catch (NoCredentialException)
			{
				Log.Debug("Canceled enter credentials");
				credentialHandler.SetConfirm(false);
			}
			catch (Exception e)
			{
				Log.Error($"Error {e}");
				credentialHandler.SetConfirm(false);
			}
		}


		public void PushRefs(string refs, ICredentialHandler credentialHandler)
		{
			try
			{
				PushOptions pushOptions = GetPushOptions(credentialHandler);

				Remote remote = repository.Network.Remotes["origin"];

				// Using a refspec, like you would use with git push...
				repository.Network.Push(remote, pushRefSpec: $"{refs}:{refs}", pushOptions: pushOptions);

				credentialHandler.SetConfirm(true);
			}
			catch (NoCredentialException)
			{
				Log.Debug("Canceled enter credentials");
				credentialHandler.SetConfirm(false);
			}
			catch (Exception e)
			{
				Log.Error($"Error {e}");
				credentialHandler.SetConfirm(false);
			}
		}


		public void PushBranch(string branchName, ICredentialHandler credentialHandler)
		{
			PushRefs($"refs/heads/{branchName}", credentialHandler);
		}


		private static PushOptions GetPushOptions(ICredentialHandler credentialHandler)
		{
			PushOptions pushOptions = new PushOptions();
			pushOptions.CredentialsProvider = (url, usernameFromUrl, types) =>
			{
				NetworkCredential credential = credentialHandler.GetCredential(url, usernameFromUrl);

				if (credential == null)
				{
					throw new NoCredentialException();
				}

				return new UsernamePasswordCredentials
				{
					Username = credential?.UserName,
					Password = credential?.Password
				};
			};

			return pushOptions;
		}


		private class NoCredentialException : Exception
		{			
		}

		public void DeleteRemoteBranch(string branchName, ICredentialHandler credentialHandler)
		{
			PushOptions pushOptions = GetPushOptions(credentialHandler);

			Remote remote = repository.Network.Remotes["origin"];

			// Using a refspec, like you would use with git push...
			repository.Network.Push(remote, pushRefSpec: $":refs/heads/{branchName}", pushOptions: pushOptions);

			credentialHandler.SetConfirm(true);
		}


		public bool TryDeleteBranch(string branchName, bool isRemote, bool isUseForce)
		{
			if (!isUseForce && !IsBranchMerged(branchName, isRemote))
			{
				return false;
			}

			repository.Branches.Remove(branchName, isRemote);

			return true;
		}


		public bool IsBranchMerged(string branchName, bool isRemote)
		{
			Branch branch = repository.Branches[isRemote ? "origin/" + branchName : branchName];

			return IsBranchMerged(branch);
		}


		private bool IsBranchMerged(Branch thisBranch)
		{
			string tipId = thisBranch.Tip.Sha;

			foreach (var branch in repository.Branches.Where(b => b!= thisBranch))
			{
				if (branch.Tip.Sha == tipId)
				{
					return true;
				}
			}

			foreach (var branch in repository.Branches.Where(b => b != thisBranch))
			{
				var commits = repository.Commits
					.QueryBy(new CommitFilter { IncludeReachableFrom = branch })
					.Where(c => c.Sha == tipId);

				if (commits.Any())
				{
					return true;
				}
			}

			return false;
		}


		public bool IsSupportedRemoteUrl()
		{
			return !repository.Network.Remotes
				.Any(remote => remote.Url.StartsWith("ssh:", StringComparison.OrdinalIgnoreCase));
		}
	}
}