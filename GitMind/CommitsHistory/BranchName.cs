using System.Windows.Input;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class BranchName : ViewModel
	{
		public BranchName(Branch branch, ICommand showBranchCommand)
		{
			Branch = branch;
			ShowBranchCommand = showBranchCommand;
		}


		public Branch Branch { get; }

		public ICommand ShowBranchCommand { get; }

		public string Text => Branch.Name;
	}
}