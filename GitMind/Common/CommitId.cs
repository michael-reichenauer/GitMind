using GitMind.Utils;


namespace GitMind.Common
{
	public class CommitId : Equatable<CommitId>
	{
		private static readonly string UncommittedId = new string('0', 40);
		private static readonly string NoneId = new string('1', 40);

		public static readonly CommitId Uncommitted = new CommitId(UncommittedId);

		public static readonly CommitId None = new CommitId(NoneId);


		public CommitId(string commitIdSha)
		{
			Id = CommitIds.GetId(commitIdSha);
			Sha = commitIdSha;
			ShortSha = Sha.Substring(0, 6);
		}


		public CommitId(int id)
		{
			Id = id;
			Sha = CommitIds.GetSha(id);
			ShortSha = Sha.Substring(0, 6);
		}


		public CommitId(int id, string sha)
		{
			Id = id;
			Sha = sha;
			ShortSha = sha.Substring(0, 6);
		}

		public int Id { get; }

		public string Sha { get; }

		public string ShortSha { get; }

		//public static implicit operator string(CommitId commitId) => commitId.Sha;

		protected override bool IsEqual(CommitId other) => Id == other.Id;

		protected override int GetHash() => Id;

		public override string ToString() => ShortSha;
	}
}
