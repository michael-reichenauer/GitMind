using GitMind.Utils;


namespace GitMind.DataModel.Private
{
	internal class MModel
	{
		public KeyedList<string, MCommit> Commits = new KeyedList<string, MCommit>(c => c.Id);
		public KeyedList<string, MSubBranch> SubBranches = new KeyedList<string, MSubBranch>(b => b.Id);	
		public KeyedList<string, MBranch> Branches = new KeyedList<string, MBranch>(b => b.Id);

		public string CurrentCommitId { get; set; }
		public string CurrentBranchId { get; set; }
	}
}
