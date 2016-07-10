using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal interface ICommitsService
	{
		void AddBranchCommits(LibGit2Sharp.Repository repo, MRepository repository);
	}
}