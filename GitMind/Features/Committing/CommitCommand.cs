using System;
using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.RepositoryViews;
using GitMind.Utils.UI;


namespace GitMind.Features.Committing
{
	internal class CommitCommand : Command
	{
		private readonly ICommitService commitService;
		private readonly Lazy<IRepositoryMgr> repositoryMgr;


		public CommitCommand(
			ICommitService commitService,
			Lazy<IRepositoryMgr> repositoryMgr)
		{
			this.commitService = commitService;
			this.repositoryMgr = repositoryMgr;
		}


		protected override Task RunAsync()
		{
			return commitService.CommitChangesAsync();
		}


		protected override bool CanRun()
		{
			Repository repository = repositoryMgr.Value.Repository;
			Commit uncommitted;
			if (repository.Commits.TryGetValue(Commit.UncommittedId, out uncommitted))
			{
				return !uncommitted.HasConflicts;
			};

			return false;
		}
	}
}
