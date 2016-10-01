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
			new StatusOptions {DetectRenamesInWorkDir = true, DetectRenamesInIndex = true};

		private static readonly MergeOptions MergeDefault =
			new MergeOptions {FastForwardStrategy = FastForwardStrategy.Default};

	
	

		public GitRepository(string workingFolder, Repository repository)
		{
			this.workingFolder = workingFolder;
			this.repository = repository;
		}


		public static GitRepository Open(string folder)
		{
			return new GitRepository(folder, new Repository(folder));
		}


		public IEnumerable<GitBranch> Branches => repository.Branches
			.Select(b => new GitBranch(b, repository));

		public IEnumerable<GitTag> Tags => repository.Tags.Select(t => new GitTag(t));

		public GitBranch Head => new GitBranch(repository.Head, repository);

		public GitStatus Status => GetGitStatus();


		public GitDiff Diff => new GitDiff(repository.Diff, repository);

		public string UserName => repository.Config.GetValueOrDefault<string>("user.name");


		public void Dispose()
		{
			repository.Dispose();
		}


		public void Fetch()
		{
			FetchOptions options = new FetchOptions {Prune = true, TagFetchMode = TagFetchMode.All};
			repository.Fetch("origin", options);
		}


		public void FetchBranch(BranchName branchName)
		{
			Remote remote = repository.Network.Remotes["origin"];

			repository.Network.Fetch(remote, new[] {$"{branchName}:{branchName}"});
		}


		public void FetchRefs(string[] refs)
		{
			try
			{
				Remote remote = repository.Network.Remotes["origin"];

				repository.Network.Fetch(remote, refs);
			}
			catch (Exception e)
			{
				Log.Error($"{e}");
				throw;
			}

		}


		public GitCommit Commit(string message)
		{
			Signature author = repository.Config.BuildSignature(DateTimeOffset.Now);
			Signature committer = repository.Config.BuildSignature(DateTimeOffset.Now);
			CommitOptions commitOptions = new CommitOptions();

			Commit commit = repository.Commit(message, author, committer, commitOptions);

			return commit != null ? new GitCommit(commit) : null;
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




		public void UndoFileInCurrentBranch(string path)
		{
			GitStatus gitStatus = GetGitStatus();

			GitFile gitFile = gitStatus.CommitFiles.Files.FirstOrDefault(f => f.File == path);

			if (gitFile != null)
			{
				if (gitFile.IsModified || gitFile.IsDeleted)
				{
					CheckoutOptions options = new CheckoutOptions {CheckoutModifiers = CheckoutModifiers.Force};
					repository.CheckoutPaths("HEAD", new[] {path}, options);
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
					CheckoutOptions options = new CheckoutOptions {CheckoutModifiers = CheckoutModifiers.Force};
					repository.CheckoutPaths("HEAD", new[] {gitFile.OldFile}, options);
				}
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


		public void PushBranch(BranchName branchName, ICredentialHandler credentialHandler)
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


		public class NoCredentialException : Exception
		{
		}




		public bool IsSupportedRemoteUrl()
		{
			return !repository.Network.Remotes
				.Any(remote => remote.Url.StartsWith("ssh:", StringComparison.OrdinalIgnoreCase));
		}


	
	}
}