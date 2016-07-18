using System.Linq;
using LibGit2Sharp;


namespace GitMind.Git
{
	internal class GitDiff
	{
		private readonly Diff diff;
		private readonly Repository repository;
		private static readonly SimilarityOptions DetectRenames =
			new SimilarityOptions { RenameDetectionMode = RenameDetectionMode.Renames };

		private static readonly CompareOptions DefultCompareOptions = new CompareOptions
			{ ContextLines = 5, Similarity = DetectRenames };
		private static readonly CompareOptions DefultFileCompareOptions = new CompareOptions
			{ ContextLines = 10000, Similarity = DetectRenames };


		public GitDiff(Diff diff, Repository repository)
		{
			this.diff = diff;
			this.repository = repository;
		}


		public string GetPatch(string commitId)
		{
			if (commitId == GitCommit.UncommittedId)
			{
				// Current working folder uncommitted changes
				return diff.Compare<Patch>(null, true, null, DefultCompareOptions);
			}

			Commit commit = repository.Lookup<Commit>(new ObjectId(commitId));

			if (commit != null)
			{
				Tree parentTree = null;
				if (commit.Parents.Any())
				{
					parentTree = commit.Parents.First().Tree;
				}

				return diff.Compare<Patch>(
					parentTree,
					commit.Tree,
					DefultCompareOptions);
			}

			return "";
		}


		internal string GetFilePatch(string commitId, string filePath)
		{
			if (commitId == GitCommit.UncommittedId)
			{
				// Current working folder uncommitted changes
				return diff.Compare<Patch>(new[] { filePath }, true, null, DefultFileCompareOptions);
			}

			Commit commit = repository.Lookup<Commit>(new ObjectId(commitId));

			if (commit != null)
			{
				Tree parentTree = null;
				if (commit.Parents.Any())
				{
					parentTree = commit.Parents.First().Tree;
				}

				return diff.Compare<Patch>(
					parentTree,
					commit.Tree,
					new[] { filePath },
					null,
					DefultFileCompareOptions);
			}

			return "";
		}


		public GitCommitFiles GetFiles(string commitId)
		{
			Commit commit = repository.Lookup<Commit>(new ObjectId(commitId));

			if (commit != null)
			{
				Tree parentTree = null;
				if (commit.Parents.Any())
				{
					parentTree = commit.Parents.First().Tree;
				}

				TreeChanges treeChanges = diff.Compare<TreeChanges>(
					parentTree,
					commit.Tree,
					DefultCompareOptions);

				return new GitCommitFiles(commitId, treeChanges);
			}

			return new GitCommitFiles(commitId, (TreeChanges)null);
		}
	}
}