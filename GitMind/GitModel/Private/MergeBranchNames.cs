using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal class MergeBranchNames
	{
		public MergeBranchNames(BranchName sourceBranchName, BranchName targetBranchName)
		{
			SourceBranchName = sourceBranchName;
			TargetBranchName = targetBranchName;
		}

		public BranchName SourceBranchName { get; }
		public BranchName TargetBranchName { get; }
	}
}