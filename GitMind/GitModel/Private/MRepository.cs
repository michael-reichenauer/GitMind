using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Features.StatusHandling;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	public class MRepository
	{
		public static string CurrentVersion = "18";

		// Serialized start -------------------

		public string Version { get; set; } = CurrentVersion;

		public int CurrentCommitId { get; set; }
		public string CurrentBranchId { get; set; }

		public List<MCommit> Commits { get; set; } = new List<MCommit>();
		
		public Dictionary<string, MBranch> Branches { get; set; } = new Dictionary<string, MBranch>();


		// Serialized Done ---------------------

		public Dictionary<string, MCommit> CommitsById { get; set; } = new Dictionary<string, MCommit>();

		public Dictionary<string, MSubBranch> SubBranches { get; set; }
			= new Dictionary<string, MSubBranch>();

		public string WorkingFolder { get; set; }

		public bool IsCached { get; set; }

		public Status Status { get; set; } = Status.Default;

		public MCommit Uncommitted { get; set; }

		public IReadOnlyList<string> RepositoryIds { get; set; } = new List<string>();


		public MCommit CurrentCommit => Commits[CurrentCommitId];
		public MBranch CurrentBranch => Branches[CurrentBranchId];
		public TimeSpan TimeToCreateFresh { get; set; }


		public MCommit Commit(int id)
		{
			return Commits[id];
		}

		public MCommit Commit(string commitId)
		{
			MCommit commit;
			if (!CommitsById.TryGetValue(commitId, out commit))
			{
				commit = AddNewCommit(commitId);
			}

			return commit;
		}

		public MCommit AddNewCommit(string commitId)
		{
			MCommit commit = new MCommit();
			commit.Repository = this;
			commit.IndexId = Commits.Count;
			commit.CommitId = commitId;
			Commits.Add(commit);

			if (commitId != null)
			{
				CommitsById[commitId] = commit;
			}

			if (commitId == MCommit.UncommittedId)
			{
				Uncommitted = commit;
			}

			return commit;
		}

		public void CompleteDeserialization(string workingFolder)
		{
			WorkingFolder = workingFolder;
			SubBranches.ForEach(b => b.Value.Repository = this);
			Branches.ForEach(b => b.Value.Repository = this);

			Log.Warn($"Commits: {Commits.Count}");

			Commits.ForEach(c =>
			{
				c.Repository = this;
				if (c.CommitId != null)
				{
					CommitsById[c.CommitId] = c;
				}
				if (c.CommitId == MCommit.UncommittedId)
				{
					Log.Warn($"Commit: {c}");
					Uncommitted = c;
				}
			});
		}
	}
}
