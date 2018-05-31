using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Utils.Git.Private;


namespace GitMind.GitModel.Private
{
	internal interface ICommitsService
	{
		void AddBranchCommits(IReadOnlyList<GitBranch> branches, MRepository repository);
		Task AddNewCommitsAsync(MRepository repository);
	}
}