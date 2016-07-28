using System;
using System.Collections.Generic;
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
		private readonly Repository repository;
		private static readonly StatusOptions StatusOptions =
			new StatusOptions { DetectRenamesInWorkDir = true, DetectRenamesInIndex = true };
		private static readonly MergeOptions MergeFastForwardOnly =
			new MergeOptions { FastForwardStrategy = FastForwardStrategy.FastForwardOnly };
		private static readonly MergeOptions MergeDefault =
			new MergeOptions { FastForwardStrategy = FastForwardStrategy.Default };
		private static readonly MergeOptions MergeNoFastForward =
			new MergeOptions { FastForwardStrategy = FastForwardStrategy.NoFastForward };



		public GitRepository(Repository repository)
		{
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


		public void Commit(string message)
		{
			Signature author = repository.Config.BuildSignature(DateTimeOffset.Now);
			Signature committer = repository.Config.BuildSignature(DateTimeOffset.Now);
			CommitOptions commitOptions = new CommitOptions();

			repository.Commit(message, author, committer, commitOptions);
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
			}
		}

		private GitStatus GetGitStatus()
		{
			RepositoryStatus repositoryStatus = repository.RetrieveStatus(StatusOptions);
			ConflictCollection conflicts = repository.Index.Conflicts;
			return new GitStatus(repositoryStatus, conflicts);
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
			repository.CheckoutPaths("HEAD", new[] { path });
		}


		public void MergeBranchNoFastForward(string branchName)
		{
			Signature committer = repository.Config.BuildSignature(DateTimeOffset.Now);

			Branch branch = repository.Branches.FirstOrDefault(b => b.FriendlyName == branchName);

			if (branch != null)
			{
				repository.Merge(branch, committer, MergeNoFastForward);
			}		
		}


		public void SwitchToCommit(string commitId)
		{
			Commit commit = repository.Lookup<Commit>(new ObjectId(commitId));
			if (commit == null)
			{
				Log.Warn("Unknown commit id {commitId}");
				return;
			}

			string shortId = commitId.Substring(0, 6);

			// Trying to create a switch branch and check out, but that branch might be "taken"
			// so we might have to retry a few times
			for (int i = 0; i < 9; i++)
			{		
				// Trying to get an existing switch branch 		
				
				Branch branch = repository.Branches.FirstOrDefault(b => !b.IsRemote && b.Tip.Id.Sha == commitId);

				string tempBranchName = $"_Switch_{shortId}{i}";
				if (branch == null)
				{
					// Try get a previous switch branch				
					branch = repository.Branches.FirstOrDefault(b => b.FriendlyName == tempBranchName);
				}

				if (branch != null && branch.Tip.Id.Sha != commitId)
				{
					// Branch name already exist, but no longer point to specified commit, lets try other name
					continue;
				}
				else if (branch == null)
				{
					// No branch with that name so lets create one
					branch = repository.Branches.Add(tempBranchName, commit);
				}

				repository.Checkout(branch);

				return;
			}

			Log.Warn("To many branches with name _{shortId}");
		}
	}
}