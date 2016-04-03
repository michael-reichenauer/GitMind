using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.DataModel.Private;
using GitMind.Git;


namespace GitMind.DataModel
{
	internal interface IModelService
	{
		Task<Model> GetModelAsync(IGitRepo gitRepo, IReadOnlyList<string> activeBranchNames);

		Task<Model> RefreshAsync(IGitRepo gitRepo, Model model);

		Task<Model> WithAddBranchNameAsync(Model model, string branchName, string commitId);

		Task<Model> WithToggleCommitAsync(Model model, Commit commit);

		Task<Model> WithRemoveBranchNameAsync(Model model, string branchName);
	}
}