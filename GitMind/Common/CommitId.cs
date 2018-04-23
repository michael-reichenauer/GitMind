using System.Runtime.Serialization;
using GitMind.Utils;


namespace GitMind.Common
{

	[DataContract]
	public class CommitId : Equatable<CommitId>
	{
		private static readonly int ShortSize = 12;
		public static readonly CommitId Uncommitted = new CommitId(CommitSha.Uncommitted);
		public static readonly CommitId None = new CommitId(CommitSha.None);
		public static readonly CommitId NoCommits = new CommitId(CommitSha.NoCommits);


		public CommitId()
		{
		}


		public CommitId(string commitSha)
			: this()
		{
			Id = commitSha.Substring(0, ShortSize);
		}

		public CommitId(CommitSha commitSha)
			: this(commitSha.Sha)
		{
		}


		[DataMember]
		public string Id { get; private set; }

		protected override bool IsEqual(CommitId other) => Id == other.Id;

		protected override int GetHash() => Id.GetHashCode();

		public override string ToString() => Id;

		public string AsText => Id;

		public static bool TryParse(string id, out CommitId commitId)
		{
			int length = id?.Length ?? 0;
			if (length < ShortSize)
			{
				commitId = null;
				return false;
			}

			commitId = new CommitId(id);
			return true;
		}
	}
}
