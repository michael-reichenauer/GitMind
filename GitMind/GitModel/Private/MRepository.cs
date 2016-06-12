using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	[DataContract]
	public class MRepository
	{
		public static string CurrentVersion = "1";

		public MRepository()
		{
			CommitList = new List<MCommit>();
			SubBrancheList = new List<MSubBranch>();
			BrancheList = new List<MBranch>();

			Commits = new KeyedList<string, MCommit>(c => c.Id);
			SubBranches = new KeyedList<string, MSubBranch>(b => b.Id);
			Branches = new KeyedList<string, MBranch>(b => b.Id);
		}


		[DataMember]
		public string Version { get; set; } = CurrentVersion;

		[DataMember]
		public DateTime Time { get; set; }
		[DataMember]
		public string CurrentCommitId { get; set; }
		[DataMember]
		public string CurrentBranchId { get; set; }
		[DataMember]
		public List<MCommit> CommitList { get; set; }
		[DataMember]
		public List<MSubBranch> SubBrancheList { get; set; }
		[DataMember]
		public List<MBranch> BrancheList { get; set; }


		public KeyedList<string, MCommit> Commits;
		public KeyedList<string, MSubBranch> SubBranches;
		public KeyedList<string, MBranch> Branches;

		public Task<IDictionary<string, IEnumerable<CommitFile>>> CommitsFilesTask { get; set; }

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
			SubBranches = new KeyedList<string, MSubBranch>(b => b.Id);
			Branches = new KeyedList<string, MBranch>(b => b.Id);

			CommitList.ForEach(c => { c.Repository = this; Commits.Add(c); });
			SubBrancheList.ForEach(b => { b.Repository = this; SubBranches.Add(b); });
			BrancheList.ForEach(b => { b.Repository = this; Branches.Add(b); });
		}
	}
}
