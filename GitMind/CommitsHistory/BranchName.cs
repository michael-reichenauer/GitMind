using GitMind.GitModel;


namespace GitMind.CommitsHistory
{
	internal class BranchName
	{
		public BranchName(Branch branch)
		{
			Branch = branch;
		}


		public Branch Branch { get; }

		public string Text => Branch.Name;
	}
}