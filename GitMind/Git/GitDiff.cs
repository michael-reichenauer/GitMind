using System.Collections.Generic;
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
				//return diff.Compare<Patch>(
				//	repository.Head.Tip.Tree,
				//	DiffTargets.WorkingDirectory,
				//	(IEnumerable<string>)null,
				//	null,
				//	DefultCompareOptions);

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

		public string GetPatchRange(string id1, string id2)
		{		
			Commit commit1 = repository.Lookup<Commit>(new ObjectId(id1));
			Commit commit2 = repository.Lookup<Commit>(new ObjectId(id2));

			if (commit1 != null && commit2 != null)
			{
				return diff.Compare<Patch>(
					commit2.Tree,
					commit1.Tree,
					DefultCompareOptions);
			}

			return "";
		}


		internal string GetFilePatch(string commitId, string filePath)
		{
			if (commitId == GitCommit.UncommittedId)
			{
				return diff.Compare<Patch>(
					repository.Head.Tip.Tree,
					DiffTargets.WorkingDirectory | DiffTargets.WorkingDirectory,
					new[] { filePath },
					null,
					DefultFileCompareOptions);

				//Current working folder uncommitted changes
				//return diff.Compare<Patch>(new[] { filePath }, true, null, DefultFileCompareOptions);
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