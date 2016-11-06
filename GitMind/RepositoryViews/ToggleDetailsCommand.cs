using System;
using System.Threading.Tasks;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class ToggleDetailsCommand : Command
	{
		private readonly Lazy<IRepositoryCommands> repositoryCommands;


		public ToggleDetailsCommand(Lazy<IRepositoryCommands> repositoryCommands)
		{
			this.repositoryCommands = repositoryCommands;
		}


		protected override Task RunAsync()
		{
			repositoryCommands.Value.IsShowCommitDetails = !repositoryCommands.Value.IsShowCommitDetails;
			return Task.CompletedTask;
		}
	}
}