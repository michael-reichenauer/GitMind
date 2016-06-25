namespace GitMind.GitModel.Private
{
	internal class SpecifiedBranch
	{
		public string CommitId { get; set; }
		public string BranchName { get; set; }
		public string SubBranchId { get; set; }


		public SpecifiedBranch(string commitId, string branchName, string subBranchId)
		{
			CommitId = commitId;
			BranchName = branchName;
			SubBranchId = subBranchId;
		}
	}
}