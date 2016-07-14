using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal interface ICommitsService
	{
		void AddBranchCommits(GitRepository gitRepository, GitStatus gitStatus, MRepository repository);
	}
}