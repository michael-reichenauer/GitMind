using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal interface ITagService
	{
		void AddTags(GitRepository repo, MRepository repository);
	}
}