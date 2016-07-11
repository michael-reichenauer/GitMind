using System;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal class TagService : ITagService
	{
		public void AddTags(GitRepository gitRepository, MRepository repository)
		{
			foreach (GitTag tag in gitRepository.Tags)
			{
				MCommit commit;
				if (repository.Commits.TryGetValue(tag.CommitId, out commit))
				{
					string name = tag.TagName;
					string tagText = $"[{name}] ";
					if (commit.Tags != null && -1 == commit.Tags.IndexOf(name, StringComparison.Ordinal))
					{
						commit.Tags += tagText;
					}
					else
					{
						commit.Tags = tagText;
					}
				}
			}
		}
	}
}