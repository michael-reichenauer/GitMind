using System;
using System.Threading.Tasks;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class OpenDetailsCommand : Command
	{
		private readonly Lazy<IRepositoryCommands> repositoryCommands;


		public OpenDetailsCommand(Lazy<IRepositoryCommands> repositoryCommands)
		{
			this.repositoryCommands = repositoryCommands;
		}


		protected override Task RunAsync()
		{
			repositoryCommands.Value.IsShowCommitDetails = true;
			return Task.CompletedTask;
		}
	}
}