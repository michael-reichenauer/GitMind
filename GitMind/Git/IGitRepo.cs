using System.Collections.Generic;


namespace GitMind.Git
{
	internal interface IGitRepo
	{
		GitCommit GetCommit(string commitId);

		GitBranch TryGetBranch(string branchName);

		GitBranch TryGetBranchByLatestCommiId(string latestCommitId);

		GitCommit GetFirstParent(GitCommit gitCommit);

		IReadOnlyList<GitTag> GetTags(string commitId);

		IReadOnlyList<string> GetCommitChildren(string commitId);

		GitBranch GetCurrentBranch();
		GitCommit GetCurrentCommit();

		IReadOnlyList<GitBranch> GetAllBranches();

		IEnumerable<GitCommit> GetAllCommts();
	}
}