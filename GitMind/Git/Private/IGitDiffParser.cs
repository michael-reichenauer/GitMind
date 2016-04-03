using System.Collections.Generic;
using System.Threading.Tasks;


namespace GitMind.Git.Private
{
	internal interface IGitDiffParser
	{
		Task<CommitDiff> ParseAsync(string commitId, IReadOnlyList<string> gitDiff);
	}
}