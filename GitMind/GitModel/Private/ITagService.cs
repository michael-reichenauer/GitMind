namespace GitMind.GitModel.Private
{
	internal interface ITagService
	{
		void AddTags(LibGit2Sharp.Repository repo, MRepository repository);
	}
}