using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.Features.Branching
{
	internal class DeleteBranchCommand : Command<Branch>
	{
		private readonly IBranchService branchService;


		public DeleteBranchCommand(IBranchService branchService)
		{
			this.branchService = branchService;
		}


		protected override Task RunAsync(Branch branch)
		{
			branchService.DeleteBranch(branch);

			return Task.CompletedTask;
		}

		protected override bool CanRun(Branch branch) => branch.IsActive;
	}
}