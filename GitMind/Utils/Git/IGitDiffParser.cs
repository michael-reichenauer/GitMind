using System.Threading.Tasks;
using GitMind.Common;
using GitMind.GitModel;


namespace GitMind.Utils.Git
{
	internal interface IGitDiffParser
	{
		Task<CommitDiff> ParseAsync(
			CommitSha commitSha, string patch, bool addPrefixes = true, bool isConflicts = false);
	}
}