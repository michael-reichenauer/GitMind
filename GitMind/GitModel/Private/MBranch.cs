using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;


namespace GitMind.GitModel.Private
{
	[DataContract]
	public class MBranch
	{
		[DataMember]
		public string Id { get; set; }
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public string LatestCommitId { get; set; }
		[DataMember]
		public string FirstCommitId { get; set; }
		[DataMember]
		public string ParentCommitId { get; set; }
		[DataMember]
		public string ParentBranchId { get; set; }
		[DataMember]
		public bool IsMultiBranch { get; set; }
		[DataMember]
		public bool IsActive { get; set; }
		[DataMember]
		public bool IsAnonymous { get; set; }
		[DataMember]
		public int LocalAheadCount { get; set; }
		[DataMember]
		public int RemoteAheadCount { get; set; }
		[DataMember]
		public bool IsLocalAndRemote { get; set; }

		[DataMember]
		public List<string> SubBrancheIds { get; set; } = new List<string>();
		[DataMember]
		public List<string> CommitIds { get; set; } = new List<string>();
		[DataMember]
		public List<string> ChildBrancheIds { get; set; } = new List<string>();

		public MRepository Repository { get; set; }

		public IEnumerable<MSubBranch> SubBranches => 
			SubBrancheIds.Select(id => Repository.SubBranches[id]);
		public IEnumerable<MCommit> Commits =>
			CommitIds.Select(id => Repository.Commits[id]);
		public IEnumerable<MBranch> ChildBranches =>
			ChildBrancheIds.Select(id => Repository.Branches[id]);

		public MCommit FirstCommit => Repository.Commits[FirstCommitId];
		public MCommit LatestCommit => Repository.Commits[LatestCommitId];
		public MCommit ParentCommit => Repository.Commits[ParentCommitId];
		public MBranch ParentBranch => Repository.Branches[ParentBranchId];


		public override string ToString() => $"{Name}";
	}
}