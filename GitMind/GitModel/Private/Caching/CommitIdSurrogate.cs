namespace GitMind.GitModel.Private.Caching
{
	//[DataContract, TypeConverter(typeof(CommitId))]
	//internal class CommitIdSurrogate
	//{
	//	[DataMember]
	//	public int Id { get; set; }

	//	public static implicit operator CommitIdSurrogate(CommitId commitId) =>
	//		commitId != null ? new CommitIdSurrogate { Id = commitId.Id } : null;

	//	public static implicit operator CommitId(CommitIdSurrogate commitId) =>
	//		commitId  != null ? new CommitId(commitId.Id) : null;
	//}
}