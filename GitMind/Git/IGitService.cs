using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Git.Private;
using GitMind.Utils;


namespace GitMind.Git
{
	internal interface IGitService
	{
		Error GitNotInstalledError { get; }
		Error GitCommandError { get; }

		Task<R<IGitRepo>> GetRepoAsync(string path);

		Task FetchAsync(string path);

		Task<R<string>> GetCurrentBranchNameAsync(string path);

		R<string> GetCurrentRootPath(string path);

		Task<R<CommitDiff>> GetCommitDiffAsync(string commitId);

		Task<R<GitStatus>> GetStatusAsync(string path);

		Task<R<IReadOnlyList<GitCommitFiles>>> GetCommitsFilesAsync(
			string path, DateTime? dateTime, int max, int skip);


		Task<R<CommitDiff>> GetCommitFileDiffAsync(string commitId, string name);
		Task<R<GitCommitFiles>> GetCommitsFilesForCommitAsync(string path, string commitId);
		Task SetSpecifiedCommitBranchAsync(string commitId, string branchName);
	}
}