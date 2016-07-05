namespace GitMind.Git
{
	internal class GitSpecifiedNames
	{
		public GitSpecifiedNames(string commitId, string branchName)
		{
			CommitId = commitId;
			BranchName = branchName;
		}

		public string CommitId { get; }

		public string BranchName { get;}
	}
}