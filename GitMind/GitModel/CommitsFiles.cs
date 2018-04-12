using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind.GitModel
{
	[SingleInstance]
	internal class CommitsFiles : ICommitsFiles
	{
		private readonly IGitCommitService2 gitCommitService2;
		private readonly IGitStatusService2 gitStatusService2;

		private readonly ConcurrentDictionary<CommitSha, IList<CommitFile>> commitsFiles =
			new ConcurrentDictionary<CommitSha, IList<CommitFile>>();

		private Task currentTask = Task.FromResult(true);
		private CommitSha nextIdToGet;


		public CommitsFiles(
			IGitCommitService2 gitCommitService2,
			IGitStatusService2 gitStatusService2)
		{
			this.gitCommitService2 = gitCommitService2;
			this.gitStatusService2 = gitStatusService2;
		}


		public async Task<IEnumerable<CommitFile>> GetAsync(CommitSha commitSha)
		{
			if (commitSha == CommitSha.Uncommitted || !commitsFiles.TryGetValue(commitSha, out var files))
			{
				nextIdToGet = commitSha;
				await currentTask;
				if (nextIdToGet != commitSha)
				{
					// This commit id is no longer relevant 
					return Enumerable.Empty<CommitFile>();
				}

				Task<R<IReadOnlyList<GitFile2>>> commitsFilesForCommitTask =
					CommitsFilesForCommitTask(commitSha);

				currentTask = commitsFilesForCommitTask;

				if ((await commitsFilesForCommitTask).HasValue(out var commitsFilesForCommit))
				{
					files = commitsFilesForCommit
						.Select(f => new CommitFile(f)).ToList();
					commitsFiles[commitSha] = files;
					return files;
				}

				Log.Error($"Failed to get files for {commitSha}");
				return Enumerable.Empty<CommitFile>();
			}

			return files;
		}


		private async Task<R<IReadOnlyList<GitFile2>>> CommitsFilesForCommitTask(CommitSha commitSha)
		{
			if (commitSha == CommitSha.Uncommitted)
			{
				R<GitStatus2> status = await gitStatusService2.GetStatusAsync(CancellationToken.None);
				if (status.IsOk)
				{
					return R.From(status.Value.Files);
				}
				else
				{
					return status.Error;
				}
			}

			return await gitCommitService2.GetCommitFilesAsync(commitSha.Sha, CancellationToken.None);
		}
	}
}