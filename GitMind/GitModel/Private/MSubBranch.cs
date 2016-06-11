using System.Runtime.Serialization;


namespace GitMind.GitModel.Private
{
	[DataContract]
	public class MSubBranch
	{
		[DataMember]
		public string Id { get; set; }
		[DataMember]
		public string BranchId { get; set; }
		[DataMember]
		public string Name { get; set; }
		[DataMember]
		public string LatestCommitId { get; set; }
		[DataMember]
		public string FirstCommitId { get; set; }
		[DataMember]
		public string ParentCommitId { get; set; }
		[DataMember]
		public bool IsMultiBranch { get; set; }
		[DataMember]
		public bool IsActive { get; set; }
		[DataMember]
		public bool IsAnonymous { get; set; }
		[DataMember]
		public bool IsRemote { get; set; }


		public MRepository Repository { get; set; }
		public MCommit FirstCommit => Repository.Commits[FirstCommitId];
		public MCommit LatestCommit => Repository.Commits[LatestCommitId];
		public MCommit ParentCommit => Repository.Commits[ParentCommitId];

		public override string ToString() => $"{Name} ({IsRemote})";
	}
}