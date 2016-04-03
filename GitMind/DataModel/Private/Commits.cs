using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.DataModel.Private
{
	internal class Commits
	{
		private readonly IGitRepo gitRepo;
		private Dictionary<string, Commit> commitIdToCommit { get; } = new Dictionary<string, Commit>();


		public Commits(IGitRepo gitRepo)
		{
			this.gitRepo = gitRepo;
		}


		public Commit GetById(string commitId)
		{
			try
			{
				return GetCommitById(commitId);
			}
			catch (Exception)
			{
				Log.Warn($"Failed to get {commitId}");
				throw;
			}
			
		}


		private Commit GetCommitById(string commitId)
		{
			Commit commit;
			if (commitIdToCommit.TryGetValue(commitId, out commit))
			{
				return commit;
			}

			GitCommit gitCommit = gitRepo.GetCommit(commitId);

			Lazy<IReadOnlyList<Commit>> parents = new Lazy<IReadOnlyList<Commit>>(
				() => gitCommit.ParentIds.Select(GetCommitById).ToList());
			Lazy<IReadOnlyList<Commit>> children = new Lazy<IReadOnlyList<Commit>>(
				() => gitRepo.GetCommitChildren(gitCommit.Id).Select(GetCommitById).ToList());

			commit = new Commit(
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