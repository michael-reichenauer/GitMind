using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitCommit : IGitCommit
	{
		private readonly IGitCmd gitCmd;
		private readonly IGitDiff gitDiff;


		public GitCommit(IGitCmd gitCmd, IGitDiff gitDiff)
		{
			this.gitCmd = gitCmd;
			this.gitDiff = gitDiff;
		}


		public Task<R<IReadOnlyList<GitFile2>>> GetCommitFilesAsync(string commit, CancellationToken ct) => 
			gitDiff.GetFilesAsync(commit, ct);
	}
}