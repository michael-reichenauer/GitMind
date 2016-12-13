using System;
using GitMind.Common;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class TagService : ITagService
	{
		private readonly IRepoCaller repoCaller;


		public TagService(IRepoCaller repoCaller)
		{
			this.repoCaller = repoCaller;
		}

		public void AddTags(GitRepository gitRepository, MRepository repository)
		{	
			repoCaller.UseLibRepo(repo =>
			{
				foreach (var tag in repo.Tags)
				{
					if (repository.Commits.TryGetValue(new CommitId(tag.Target.Sha), out MCommit commit))
					{
						string name = tag.FriendlyName;
						string tagText = $"[{name}] ";
						if (commit.Tags != null && -1 == commit.Tags.IndexOf(name, StringComparison.Ordinal))
						{
							commit.Tags += tagText;
						}
						else if (commit.Tags == null)
						{
							commit.Tags = tagText;
						}
					}
				}
			});
		}
	}
}