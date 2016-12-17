using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using GitMind.Utils;


namespace GitMind.Common
{
	[DataContract]
	public class CommitId : Equatable<CommitId>
	{
		private static readonly string UncommittedId = new string('0', 40);
		private static readonly string NoneId = new string('1', 40);

		public static readonly CommitId Uncommitted = new CommitId(UncommittedId);

		public static readonly CommitId None = new CommitId(NoneId);
		private readonly Lazy<string> shortSha;


		public CommitId()
		{
			shortSha = new Lazy<string>(() => Sha.Substring(0, 6));
		}

		public CommitId(string commitIdSha)
			: this()
		{
			// Id = CommitIds.GetId(commitIdSha);
			Sha = commitIdSha;

			//ShortSha = Sha.Substring(0, 6);
		}


		//public CommitId(int id)
		//{
		//	//Id = id;
		//	Sha = CommitIds.GetSha(id);
		//	ShortSha = Sha.Substring(0, 6);
		//}


		//public CommitId(int id, string sha)
		//{
		//	Id = id;
		//	Sha = sha;
		//	ShortSha = sha.Substring(0, 6);
		//}

		//public int Id { get; }

		[DataMember]
		public string Sha { get; private set; }

		public string ShortSha => shortSha.Value;

		//public static implicit operator string(CommitId commitId) => commitId.Sha;

		protected override bool IsEqual(CommitId other) => Sha == other.Sha;

		protected override int GetHash() => Sha.GetHashCode();

		public override string ToString() => ShortSha;
	}
}
