using System;
using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.RepositoryViews;
using GitMind.Utils.UI;


namespace GitMind.Features.Branches
{
	internal class MergeCommand : Command<Branch>
	{
		private readonly IBranchService branchService;
		private readonly OpenDetailsCommand openDetailsCommand;
		private readonly Lazy<IRepositoryMgr> repositoryMgr;


		public MergeCommand(
			IBranchService branchService,
			OpenDetailsCommand openDetailsCommand,
			Lazy<IRepositoryMgr> repositoryMgr)
		{
			this.branchService = branchService;
			this.openDetailsCommand = openDetailsCommand;
			this.repositoryMgr = repositoryMgr;
		}


		protected override async Task RunAsync(Branch branch)
		{
			await branchService.MergeBranchAsync(branch);

			Repository repository = repositoryMgr.Value.Repository;

			if (repository.Status.ConflictCount > 0)
			{
				await openDetailsCommand.ExecuteAsync();
			}
		}
	}
}