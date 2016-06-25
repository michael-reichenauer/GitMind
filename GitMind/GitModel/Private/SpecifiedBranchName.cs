namespace GitMind.GitModel.Private
{
	internal class SpecifiedBranchName
	{
		public string CommitId { get; set; }
		public string BranchName { get; set; }

		public SpecifiedBranchName(string commitId, string branchName)
		{
			CommitId = commitId;
			BranchName = branchName;
		}
	}
}