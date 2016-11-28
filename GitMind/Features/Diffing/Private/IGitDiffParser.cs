using System.Threading.Tasks;
using GitMind.Git;


namespace GitMind.Features.Diffing.Private
{
	internal interface IGitDiffParser
	{
		Task<CommitDiff> ParseAsync(string commitId, string patch, bool addPrefixes = true);
	}
}