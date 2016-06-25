using System.Collections.Generic;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal interface ITagService
	{
		void AddTags(IReadOnlyList<GitTag> tags, MRepository mRepository);
	}
}