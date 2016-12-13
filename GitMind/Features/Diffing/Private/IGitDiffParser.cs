using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Git;


namespace GitMind.Features.Diffing.Private
{
	internal interface IGitDiffParser
	{
		Task<CommitDiff> ParseAsync(CommitId commitId, string patch, bool addPrefixes = true);
	}
}