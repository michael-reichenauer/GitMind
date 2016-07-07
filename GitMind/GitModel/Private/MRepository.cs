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
		public static string CurrentVersion = "4";


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


		public KeyedList<string, MCommit> Commits = new KeyedList<string, MCommit>(c => c.Id);
		public KeyedList<string, MSubBranch> SubBranches
			= new KeyedList<string, MSubBranch>(b => b.SubBranchId);
		public KeyedList<string, MBranch> Branches = new KeyedList<string, MBranch>(b => b.Id);

		internal CommitsFiles CommitsFiles { get; set; }

		public MCommit CurrentCommit => Commits[CurrentCommitId];
		public MBranch CurrentBranch => Branches[CurrentBranchId];

		public void PrepareForSerialization()
		{
			Commits.ForEach(c => CommitList.Add(c));
			SubBranches.ForEach(b => SubBrancheList.Add(b));
			Branches.ForEach(b => BrancheList.Add(b));
		}

		public void CompleteDeserialization()
		{
			Commits = new KeyedList<string, MCommit>(c => c.Id);
			SubBranches = new KeyedList<string, MSubBranch>(b => b.SubBranchId);
			Branches = new KeyedList<string, MBranch>(b => b.Id);

			CommitList.ForEach(c => { c.Repository = this; Commits.Add(c); });
			SubBrancheList.ForEach(b => { b.Repository = this; SubBranches.Add(b); });
			BrancheList.ForEach(b => { b.Repository = this; Branches.Add(b); });
		}
	}
}
