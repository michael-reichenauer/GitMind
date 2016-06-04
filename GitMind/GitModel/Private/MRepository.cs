using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class MRepository
	{
		public KeyedList<string, MCommit> Commits = new KeyedList<string, MCommit>(c => c.Id);
		public KeyedList<string, MSubBranch> SubBranches = new KeyedList<string, MSubBranch>(b => b.Id);	
		public KeyedList<string, MBranch> Branches = new KeyedList<string, MBranch>(b => b.Id);

		public MCommit CurrentCommit { get; set; }
		public MBranch CurrentBranch { get; set; }
	}
}
