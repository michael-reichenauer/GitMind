using System.Threading.Tasks;


namespace GitMind.Git.Private
{
	internal interface IGitDiffParser
	{
		Task<CommitDiff> ParseAsync(string commitId, string patch, bool addPrefixes = true);
	}
}