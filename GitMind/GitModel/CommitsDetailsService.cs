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
	internal class CommitsDetailsService : ICommitsDetailsService
	{
		private readonly IGitCommitService gitCommitService;
		private readonly IGitStatusService2 gitStatusService2;

		private readonly ConcurrentDictionary<CommitSha, CommitDetails> commitsFiles =
			new ConcurrentDictionary<CommitSha, CommitDetails>();

		private Task currentTask = Task.FromResult(true);
		private CommitSha nextIdToGet;


		public CommitsDetailsService(
			IGitCommitService gitCommitService,
			IGitStatusService2 gitStatusService2)
		{
			this.gitCommitService = gitCommitService;
			this.gitStatusService2 = gitStatusService2;
		}


		public async Task<CommitDetails> GetAsync(CommitSha commitSha, GitStatus2 status)
		{
			if (commitSha == CommitSha.NoCommits)
			{
				return new CommitDetails(new CommitFile[0], null);
			}

			// Get fresh list of uncomitted files or try to get them from cach, otherwise get from repo
			if (commitSha == CommitSha.Uncommitted || !commitsFiles.TryGetValue(commitSha,out CommitDetails commitDetails))
			{
				nextIdToGet = commitSha;
				await currentTask;
				if (nextIdToGet != commitSha)
				{
					// This commit id is no longer relevant 
					return new CommitDetails(new CommitFile[0], null);
				}

				string message = (commitSha == CommitSha.Uncommitted || commitSha == CommitSha.NoCommits)
					? null
					: (await gitCommitService.GetCommitMessageAsync(commitSha.Sha, CancellationToken.None)).Or(null);

				Task<R<IReadOnlyList<GitFile2>>> commitsFilesForCommitTask =
					CommitsFilesForCommitTask(commitSha, status);

				GitConflicts conflicts = GitConflicts.None;
				if (commitSha == CommitSha.Uncommitted && status.HasConflicts)
				{
					conflicts = (await gitStatusService2.GetConflictsAsync(CancellationToken.None)).Or(GitConflicts.None);
				}

				currentTask = commitsFilesForCommitTask;

				if ((await commitsFilesForCommitTask).HasValue(out var commitsFilesForCommit))
				{
					var files = commitsFilesForCommit.Select(f =>
						{
							GitConflictFile conflict = conflicts.Files.FirstOrDefault(cf => cf.FilePath == f.FilePath);
							return new CommitFile(f, conflict);
						})
					.ToList();

					commitDetails = new CommitDetails(files, message);
					// Cache the list of files
					commitsFiles[commitSha] = commitDetails;
					return commitDetails;
				}

				Log.Error($"Failed to get files for {commitSha}");
				return new CommitDetails(new CommitFile[0], message);
			}

			return commitDetails;
		}


		private async Task<R<IReadOnlyList<GitFile2>>> CommitsFilesForCommitTask(CommitSha commitSha, GitStatus2 status)
		{
			if (commitSha == CommitSha.Uncommitted)
			{
				return R.From(status.Files);
			}

			return await gitCommitService.GetCommitFilesAsync(commitSha.Sha, CancellationToken.None);
		}
	}
}