using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Utils;
using ProtoBuf;


namespace GitMind.GitModel.Private
{
	[ProtoContract]
	public class MRepository
	{
		public static string CurrentVersion = "6";

		private readonly Dictionary<string, MCommit> commitById = new Dictionary<string, MCommit>();


		[ProtoMember(1)]
		public string Version { get; set; } = CurrentVersion;

		[ProtoMember(2)]
		public DateTime Time { get; set; }
		[ProtoMember(3)]
		public string CurrentCommitId { get; set; }
		[ProtoMember(4)]
		public string CurrentBranchId { get; set; }
		[ProtoMember(5)]
		public List<MCommit> CommitList { get; set; } = new List<MCommit>();
		[ProtoMember(6)]
		public List<MSubBranch> SubBrancheList { get; set; } = new List<MSubBranch>();
		[ProtoMember(7)]
		public List<MBranch> BrancheList { get; set; } = new List<MBranch>();
		[ProtoMember(8)]
		public readonly Dictionary<string, IList<string>> ChildrenById =
			new Dictionary<string, IList<string>>();
		[ProtoMember(9)]
		public readonly Dictionary<string, IList<string>> FirstChildrenById =
			new Dictionary<string, IList<string>>();


		public MCommit Commits(string commitId)
		{
			return commitById[commitId];
		}


		public void AddCommit(MCommit commit)
		{
			CommitList.Add(commit);
			commitById[commit.Id] = commit;
		}

		public bool TryGetCommit(string commitId, out MCommit commit)
		{
			return commitById.TryGetValue(commitId, out commit);
		}

		public bool CommitExists(string commitId)
		{
			return commitById.ContainsKey(commitId);
		}



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


		public KeyedList<string, MSubBranch> SubBranches
			= new KeyedList<string, MSubBranch>(b => b.SubBranchId);
		public KeyedList<string, MBranch> Branches = new KeyedList<string, MBranch>(b => b.Id);

		internal CommitsFiles CommitsFiles { get; set; }

		public MCommit CurrentCommit => Commits(CurrentCommitId);
		public MBranch CurrentBranch => Branches[CurrentBranchId];

		public void PrepareForSerialization()
		{
			SubBranches.ForEach(b => SubBrancheList.Add(b));
			Branches.ForEach(b => BrancheList.Add(b));
		}

		public void CompleteDeserialization()
		{
			SubBranches = new KeyedList<string, MSubBranch>(b => b.SubBranchId);
			Branches = new KeyedList<string, MBranch>(b => b.Id);

			CommitList.ForEach(c => { c.Repository = this; commitById[c.Id] = c; });
			SubBrancheList.ForEach(b => { b.Repository = this; SubBranches.Add(b); });
			BrancheList.ForEach(b => { b.Repository = this; Branches.Add(b); });
		}
	}
}
