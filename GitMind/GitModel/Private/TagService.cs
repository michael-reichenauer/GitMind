using System;


namespace GitMind.GitModel.Private
{
	internal class TagService : ITagService
	{
		public void AddTags(LibGit2Sharp.Repository repo, MRepository repository)
		{
			foreach (LibGit2Sharp.Tag tag in repo.Tags)
			{
				MCommit commit;
				if (repository.Commits.TryGetValue(tag.Target.Sha, out commit))
				{
					string name = tag.FriendlyName;
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