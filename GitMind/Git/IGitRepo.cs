using System.Collections.Generic;


namespace GitMind.Git
{
	internal interface IGitRepo
	{
		//GitCommit CurrentCommit { get; }
		//GitBranch CurrentBranch { get; }

		//GitCommit GetCommit(string commitId);

		//GitBranch TryGetBranch(string branchName);

		//GitBranch TryGetBranchByLatestCommiId(string latestCommitId);

		//GitCommit GetFirstParent(GitCommit gitCommit);

		//IReadOnlyList<GitTag> GetTags(string commitId);

		IReadOnlyList<GitSpecifiedNames> GetSpecifiedNameses();

		//IReadOnlyList<string> GetCommitChildren(string commitId);

		//GitBranch GetCurrentBranch();


		//IReadOnlyList<GitBranch> GetAllBranches();

		//IReadOnlyList<GitTag> GetAllTags();
	}
}