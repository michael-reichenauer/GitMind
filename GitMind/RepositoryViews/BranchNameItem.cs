using System.Windows.Input;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class BranchNameItem : ViewModel
	{
		private readonly string commitId;
		private readonly Command<string> command;


		public BranchNameItem(string commitId, string name, Command<string> command)
		{
			this.commitId = commitId;
			this.command = command;
			Name = name;
		}


		public string Name { get; }

		public Command ItemCommand => Command(SpecifyName);


		private void SpecifyName()
		{
			command.Execute(commitId + "," + Name);
		}
	}
}