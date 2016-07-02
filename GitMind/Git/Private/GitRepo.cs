using System.Collections.Generic;
using System.Linq;


namespace GitMind.Git.Private
{
	internal class GitRepo : IGitRepo
	{
		private readonly IReadOnlyList<GitTag> tags;
		private readonly IReadOnlyList<GitSpecifiedNames> specifiedNames;
		private static readonly IReadOnlyList<GitTag> noTags = new List<GitTag>();

		private readonly Dictionary<string, GitCommit> commits = new Dictionary<string, GitCommit>();

		private readonly Dictionary<string, List<string>> commitIdToChildren =
			new Dictionary<string, List<string>>();

		private readonly Dictionary<string, List<GitTag>> commitIdToTags =
			new Dictionary<string, List<GitTag>>();

		private readonly List<GitBranch> branches = new List<GitBranch>();


		public GitRepo(
			IReadOnlyList<GitBranch> branches, 
			IReadOnlyList<GitCommit> commits, 
			IReadOnlyList<GitTag> tags,
			IReadOnlyList<GitSpecifiedNames> specifiedNames,
			GitCommit currentCommit,
			GitBranch currentBranch)
		{
			this.tags = tags;
			this.specifiedNames = specifiedNames;
			CurrentCommit = currentCommit;
			CurrentBranch = currentBranch;

			SetGitBranches(branches);

			SetGitCommits(commits);

			SetTags(tags);
		}


		public GitCommit CurrentCommit { get; }

		public GitBranch CurrentBranch { get; }

		public IEnumerable<GitCommit> GetAllCommts() => commits.Values;

		public IReadOnlyList<GitSpecifiedNames> GetSpecifiedNameses() => specifiedNames;


		public IReadOnlyList<string> GetCommitChildren(string commitId) => GetChildren(commitId);


		public GitCommit GetCommit(string commitId)
		{
			GitCommit commit;
			if (commits.TryGetValue(commitId, out commit))
			{
				return commit;
			}

			return GitCommit.None;
		}


		public IReadOnlyList<GitTag> GetTags(string commitId)
		{
			List<GitTag> tags;
			if (commitIdToTags.TryGetValue(commitId, out tags))
			{
				return tags;
			}

			return noTags;
		}


		public IReadOnlyList<GitTag> GetAllTags() => tags;


		public GitBranch GetCurrentBranch()
		{
			return branches.First(branch => branch.IsCurrent);
		}


		public IReadOnlyList<GitBranch> GetAllBranches()
		{
			return branches;
		}


		public GitBranch TryGetBranchByLatestCommiId(string latestCommitId)
		{
			return branches.FirstOrDefault(branch => branch.LatestCommitId == latestCommitId);
		}


		public GitCommit GetFirstParent(GitCommit commit)
		{
			return commit.ParentIds.Count > 0
				? commits[commit.ParentIds[0]]
				: GitCommit.None;
		}


		public GitBranch TryGetBranch(string branchName)
		{
			return branches.FirstOrDefault(branch => branch.Name == branchName);
		}


		private void SetGitBranches(IReadOnlyList<GitBranch> newGitBranches)
		{
			foreach (GitBranch branch in newGitBranches)
			{
				branches.Add(branch);
			}
		}


		private void SetGitCommits(IReadOnlyList<GitCommit> commits)
		{
			foreach (GitCommit gitCommit in commits)
			{
				this.commits[gitCommit.Id] = gitCommit;

				UpdateChildren(gitCommit);
			}
		}


		private void SetTags(IReadOnlyList<GitTag> tags)
		{
			foreach (GitTag tag in tags)
			{
				List<GitTag> commitTags;
				if (!commitIdToTags.TryGetValue(tag.CommitId, out commitTags))
				{
					commitTags = new List<GitTag>();
				}

				commitTags.Add(tag);

				commitIdToTags[tag.CommitId] = commitTags;
			}
		}

		private void UpdateChildren(GitCommit gitCommit)
		{
			foreach (string parentId in gitCommit.ParentIds)
			{
				List<string> parentChildren = GetChildren(parentId);

				if (!parentChildren.Contains(gitCommit.Id))
				{
					parentChildren.Add(gitCommit.Id);
				}
			}
		}


		private List<string> GetChildren(string commitId)
		{
			List<string> commitChildren;
			if (!commitIdToChildren.TryGetValue(commitId, out commitChildren))
			{
				commitChildren = new List<string>();
				commitIdToChildren[commitId] = commitChildren;
			}

			return commitChildren;
		}
	}
}