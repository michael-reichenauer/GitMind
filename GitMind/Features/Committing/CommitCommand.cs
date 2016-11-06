using System;
using System.Threading.Tasks;
using GitMind.Common.MessageDialogs;
using GitMind.GitModel;
using GitMind.RepositoryViews;
using GitMind.Utils.UI;


namespace GitMind.Features.Committing
{
	internal class CommitCommand : Command
	{
		private readonly ICommitService commitService;
		private readonly Lazy<IRepositoryMgr> repositoryMgr;
		private readonly IMessage message;
		private readonly OpenDetailsCommand openDetailsCommand;


		public CommitCommand(
			ICommitService commitService,
			Lazy<IRepositoryMgr> repositoryMgr,
			IMessage message,
			OpenDetailsCommand openDetailsCommand)
		{
			this.commitService = commitService;
			this.repositoryMgr = repositoryMgr;
			this.message = message;
			this.openDetailsCommand = openDetailsCommand;
		}


		protected override async Task RunAsync()
		{
			Repository repository = repositoryMgr.Value.Repository;
			Commit uncommitted;
			if (repository.Commits.TryGetValue(Commit.UncommittedId, out uncommitted))
			{
				if (uncommitted.HasConflicts)
				{
					message.ShowInfo("There are merge conflicts that needs be resolved before committing.");
					await openDetailsCommand.ExecuteAsync();
					return;
				}
			}
			else
			{
				message.ShowInfo("No changes, nothing to commit.");
				return;
			}

			await commitService.CommitChangesAsync();
		}
	}
}
