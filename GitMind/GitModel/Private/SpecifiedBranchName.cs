using GitMind.Utils.Git;


namespace GitMind.GitModel.Private
{
	internal class SpecifiedBranchName
	{
		public string CommitId { get; set; }
		public BranchName BranchName { get; set; }

		public SpecifiedBranchName(string commitId, BranchName branchName)
		{
			CommitId = commitId;
			BranchName = branchName;
		}
	}
}