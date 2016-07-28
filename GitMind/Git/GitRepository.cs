using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.GitModel;
using LibGit2Sharp;
using Branch = LibGit2Sharp.Branch;
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
	}
}