using System.Collections.Generic;
using System.Threading.Tasks;


namespace GitMind.DataModel.Old
{
	internal interface IModelService
	{
		Task<Model> GetCachedModelAsync(IReadOnlyList<string> activeBranchNames);

		Task<Model> RefreshAsync(Model model);

		Task<Model> WithAddBranchNameAsync(Model model, string branchName, string commitId);

		Task<Model> WithToggleCommitAsync(Model model, Commit commit);

		Task<Model> WithRemoveBranchNameAsync(Model model, string branchName);
	}
}