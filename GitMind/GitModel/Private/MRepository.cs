using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Git;
using ProtoBuf;


namespace GitMind.GitModel.Private
{
	[ProtoContract]
	public class MRepository
	{
		public static string CurrentVersion = "11";

		[ProtoMember(1)]
		public string Version { get; set; } = CurrentVersion;

		[ProtoMember(2)]
		public string CurrentCommitId { get; set; }
		[ProtoMember(3)]
		public string CurrentBranchId { get; set; }
		[ProtoMember(4)]
		public Dictionary<string, MCommit> Commits { get; set; } = new Dictionary<string, MCommit>();		
		[ProtoMember(5)]
		public Dictionary<string, MBranch> Branches { get; set; } = new Dictionary<string, MBranch>();
		[ProtoMember(6)]
		public readonly Dictionary<string, IList<string>> ChildrenById =
			new Dictionary<string, IList<string>>();
		[ProtoMember(7)]
		public readonly Dictionary<string, IList<string>> FirstChildrenById =
			new Dictionary<string, IList<string>>();


		public Dictionary<string, MSubBranch> SubBranches { get; set; } 
			= new Dictionary<string, MSubBranch>();

		public string WorkingFolder { get; set; }

		public GitStatus Status { get; set; }

		public IList<string> ChildIds(string commitId)
		{
			IList<string> children;
			if (!ChildrenById.TryGetValue(commitId, out children) || children == null)
			{
				children = new List<string>();
				ChildrenById[commitId] = children;
			}

			return children;
		}


		public IList<string> FirstChildIds(string commitId)
		{
			IList<string> children;
			if (!FirstChildrenById.TryGetValue(commitId, out children) || children == null)
			{
				children = new List<string>();
				FirstChildrenById[commitId] = children;
			}

			return children;
		}


		internal CommitsFiles CommitsFiles { get; set; } = new CommitsFiles();
		public MCommit CurrentCommit => Commits[CurrentCommitId];
		public MBranch CurrentBranch => Branches[CurrentBranchId];


		public void CompleteDeserialization(string gitRepositoryPath)
		{
			WorkingFolder = gitRepositoryPath;
			Commits.ForEach(c => c.Value.Repository = this);
			SubBranches.ForEach(b => b.Value.Repository = this);
			Branches.ForEach(b => b.Value.Repository = this);
		}
	}
}