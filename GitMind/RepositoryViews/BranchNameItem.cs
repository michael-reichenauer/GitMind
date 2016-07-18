using System.Windows.Input;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class BranchNameItem : ViewModel
	{
		private readonly string commitId;
		private readonly ICommand command;


		public BranchNameItem(string commitId, string name, ICommand command)
		{
			this.commitId = commitId;
			this.command = command;
			Name = name;
		}


		public string Name { get; }

		public ICommand ItemCommand => Command(SpecifyName);


		private void SpecifyName()
		{
			command.Execute(commitId + "," + Name);
		}
	}
}