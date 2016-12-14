using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Common;
using GitMind.Features.StatusHandling;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	public class MRepository
	{
		public static string CurrentVersion = "19";

		// Serialized start -------------------

		public string Version { get; set; } = CurrentVersion;

		public CommitIntBySha CommitIntBySha { get; set; } = new CommitIntBySha(1);

		public CommitId CurrentCommitId { get; set; }

		public string CurrentBranchId { get; set; }

		public Dictionary<CommitId, MCommit> Commits { get; set; } = new Dictionary<CommitId, MCommit>();
		
		public Dictionary<string, MBranch> Branches { get; set; } = new Dictionary<string, MBranch>();
		public TimeSpan TimeToCreateFresh { get; set; }

		// Serialized Done ---------------------

		//public Dictionary<string, CommitId> CommitIdsBySha { get; set; } = new Dictionary<string, CommitId>();

		public Dictionary<string, MSubBranch> SubBranches { get; set; }
			= new Dictionary<string, MSubBranch>();

		public string WorkingFolder { get; set; }

		public bool IsCached { get; set; }

		public Status Status { get; set; } = Status.Default;

		public MCommit Uncommitted { get; set; }

		public IReadOnlyList<string> RepositoryIds { get; set; } = new List<string>();


		public MCommit CurrentCommit => Commits[CurrentCommitId];
		public MBranch CurrentBranch => Branches[CurrentBranchId];
		

		public MCommit Commit(string shaId)
		{
			CommitId commitId = new CommitId(shaId);
			MCommit commit;
			if (!Commits.TryGetValue(commitId, out commit))
			{
				commit = AddNewCommit(commitId);
			}

			return commit;
		}


		public MCommit AddNewCommit(CommitId commitId)
		{
			MCommit commit = new MCommit()
			{
				Repository = this,
				Id = commitId,
				ViewCommitId = commitId
			};

			Commits[commitId] = commit;

			return commit;
		}


		public MCommit AddVirtualCommit(CommitId realCommitId)
		{
			string idText = (Guid.NewGuid() + Guid.NewGuid().ToString()).Replace("-", "")
				.Substring(0, 40);
			CommitId commitId = new CommitId(idText);
			MCommit commit = new MCommit()
			{
				Repository = this,
				Id = commitId,
				ViewCommitId = realCommitId
			};

			Commits[commitId] = commit;

			return commit;
		}


		public void CompleteDeserialization(string workingFolder)
		{
			WorkingFolder = workingFolder;
			SubBranches.ForEach(b => b.Value.Repository = this);
			Branches.ForEach(b => b.Value.Repository = this);

			Commits.Values.ForEach(c =>
			{
				c.Repository = this;
			});
		}
	}
}
