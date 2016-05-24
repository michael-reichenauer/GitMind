using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Git;


namespace GitMind.DataModel.Old
{
	internal class Commits
	{
		private readonly IGitRepo gitRepo;
		private Dictionary<string, OldCommit> commitIdToCommit { get; } = new Dictionary<string, OldCommit>();


		public Commits(IGitRepo gitRepo)
		{
			this.gitRepo = gitRepo;
		}


		public OldCommit GetById(string commitId)
		{
			OldCommit commit;
			if (commitIdToCommit.TryGetValue(commitId, out commit))
			{
				return commit;
			}

			GitCommit gitCommit = gitRepo.GetCommit(commitId);

			Lazy<IReadOnlyList<OldCommit>> parents = new Lazy<IReadOnlyList<OldCommit>>(
				() => gitCommit.ParentIds.Select(GetById).ToList());
			Lazy<IReadOnlyList<OldCommit>> children = new Lazy<IReadOnlyList<OldCommit>>(
				() => gitRepo.GetCommitChildren(gitCommit.Id).Select(GetById).ToList());

			commit = new OldCommit(
				gitCommit.Id,
				parents,
				children,
				gitCommit.Subject,
				gitCommit.Author,
				gitCommit.DateTime,
				gitCommit.CommitDate,
				gitCommit.BranchName);

			commitIdToCommit[commit.Id] = commit;

			return commit;
		}
	}
}