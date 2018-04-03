using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Features.StatusHandling;
using GitMind.Git;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind.GitModel
{
	[SingleInstance]
	internal class CommitsFiles : ICommitsFiles
	{
		private readonly IGitCommitsService gitCommitsService;

		private readonly ConcurrentDictionary<CommitSha, IList<CommitFile>> commitsFiles =
			new ConcurrentDictionary<CommitSha, IList<CommitFile>>();

		private Task currentTask = Task.FromResult(true);
		private CommitSha nextIdToGet;


		public CommitsFiles(IGitCommitsService gitCommitsService)
		{
			this.gitCommitsService = gitCommitsService;
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
					gitCommitsService.GetFilesForCommitAsync(commitSha);

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
	}
}