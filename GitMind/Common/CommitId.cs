using System;
using System.Runtime.Serialization;
using GitMind.Utils;


namespace GitMind.Common
{
	[DataContract]
	public class CommitId : Equatable<CommitId>
	{
		public static readonly CommitId Uncommitted = new CommitId(new string('0', 40));
		public static readonly CommitId None = new CommitId(new string('1', 40));

		private readonly Lazy<string> shortSha;

		public CommitId()
		{
			shortSha = new Lazy<string>(() => Sha.Substring(0, 6));
		}

		public CommitId(string commitSha)
			: this()
		{
			Sha = commitSha;
		}


		[DataMember]
		public string Sha { get; private set; }

		public string ShortSha => shortSha.Value;

		//public static implicit operator string(CommitId commitId) => commitId.Sha;

		protected override bool IsEqual(CommitId other) => Sha == other.Sha;

		protected override int GetHash() => Sha.GetHashCode();

		public override string ToString() => ShortSha;
	}
}
