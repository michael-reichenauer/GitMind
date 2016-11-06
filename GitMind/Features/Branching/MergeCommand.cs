using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.Features.Branching
{
	internal class MergeCommand : Command<Branch>
	{
		private readonly IBranchService branchService;
		//private readonly Lazy<IRepositoryMgr> repositoryMgr;


		public MergeCommand(
			IBranchService branchService)
		//Lazy<IRepositoryMgr> repositoryMgr)
		{
			this.branchService = branchService;
			//this.repositoryMgr = repositoryMgr;

			SetCommand(MergeAsync, nameof(MergeCommand));
		}


		private Task MergeAsync(Branch branch)
		{
			return branchService.MergeBranchAsync(branch);

			// Repository repository = repositoryMgr.Value.Repository;

			//if (repository.Status.ConflictCount > 0)
			//{
			//	IsShowCommitDetails = true;
			//}
		}
	}
}