using System;
using System.Collections.Generic;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal class TagService : ITagService
	{
		public void AddTags(IReadOnlyList<GitTag> tags, MRepository mRepository)
		{
			foreach (GitTag tag in tags)
			{
				MCommit commit;
				if (mRepository.Commits.TryGetValue(tag.CommitId, out commit))
				{
					string tagText = $"[{tag.TagName}] ";
					if (commit.Tags != null && -1 == commit.Tags.IndexOf(tag.TagName, StringComparison.Ordinal))
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