using System.Collections.Generic;
using System.Threading.Tasks;


namespace GitMind.DataModel.Old
{
	internal interface IOldModelService
	{
		Task<OldModel> GetCachedModelAsync(IReadOnlyList<string> activeBranchNames);

		Task<OldModel> RefreshAsync(OldModel model);

		Task<OldModel> WithAddBranchNameAsync(OldModel model, string branchName, string commitId);

		Task<OldModel> WithToggleCommitAsync(OldModel model, OldCommit commit);

		Task<OldModel> WithRemoveBranchNameAsync(OldModel model, string branchName);
	}
}