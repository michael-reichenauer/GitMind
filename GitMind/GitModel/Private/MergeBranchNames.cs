namespace GitMind.GitModel.Private
{
	internal class MergeBranchNames
	{
		public MergeBranchNames(string sourceBranchName, string targetBranchName)
		{
			SourceBranchName = sourceBranchName;
			TargetBranchName = targetBranchName;
		}

		public string SourceBranchName { get; }
		public string TargetBranchName { get; }
	}
}