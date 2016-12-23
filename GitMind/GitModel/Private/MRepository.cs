using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using GitMind.Common;
using GitMind.Features.StatusHandling;


namespace GitMind.GitModel.Private
{
	[DataContract]
	public class MRepository
	{
		public static string CurrentVersion = "20";

		[DataMember] public string Version { get; set; } = CurrentVersion;

		[DataMember] public Dictionary<CommitId, GitCommit> GitCommits { get; set; } =
			new Dictionary<CommitId, GitCommit>();
		[DataMember] public Dictionary<CommitId, MCommit> Commits { get; set; } = 
			new Dictionary<CommitId, MCommit>();
		[DataMember] public CommitId CurrentCommitId { get; set; }
		[DataMember] public string CurrentBranchId { get; set; }
		[DataMember] public Dictionary<string, MBranch> Branches { get; set; } = 
			new Dictionary<string, MBranch>();
		[DataMember] public TimeSpan TimeToCreateFresh { get; set; }


		public Dictionary<string, MSubBranch> SubBranches { get; set; }
			= new Dictionary<string, MSubBranch>();

		public string WorkingFolder { get; set; }

		public bool IsCached { get; set; }

		public Status Status { get; set; } = Status.Default;

		public MCommit Uncommitted { get; set; }

		public IReadOnlyList<string> RepositoryIds { get; set; } = new List<string>();


		public MCommit CurrentCommit => Commits[CurrentCommitId];
		public MBranch CurrentBranch => Branches[CurrentBranchId];

		public MCommit Commit(CommitId commitId)
		{
			MCommit commit;
			if (!Commits.TryGetValue(commitId, out commit))
			{
				commit = AddNewCommit(commitId);
			}

			return commit;
		}


		private MCommit AddNewCommit(CommitId commitId)
		{
			MCommit commit = new MCommit()
			{
				Repository = this,
				Id = commitId,
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
