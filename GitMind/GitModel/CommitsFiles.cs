using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Features.StatusHandling;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.GitModel
{
	[SingleInstance]
	internal class CommitsFiles : ICommitsFiles
	{
		private readonly IGitCommitsService gitCommitsService;

		private readonly ConcurrentDictionary<CommitId, IList<CommitFile>> commitsFiles =
			new ConcurrentDictionary<CommitId, IList<CommitFile>>();

		private Task currentTask = Task.FromResult(true);
		private CommitId nextIdToGet;


		public CommitsFiles(IGitCommitsService gitCommitsService)
		{
			this.gitCommitsService = gitCommitsService;
		}


		public async Task<IEnumerable<CommitFile>> GetAsync(CommitId commitId)
		{
			if (commitId == CommitId.Uncommitted || !commitsFiles.TryGetValue(commitId, out var files))
			{
				nextIdToGet = commitId;
				await currentTask;
				if (nextIdToGet != commitId)
				{
					// This commit id is no longer relevant 
					return Enumerable.Empty<CommitFile>();
				}

				Task<R<IReadOnlyList<StatusFile>>> commitsFilesForCommitTask =
					gitCommitsService.GetFilesForCommitAsync(commitId);

				currentTask = commitsFilesForCommitTask;
				
				if ((await commitsFilesForCommitTask).HasValue(out var commitsFilesForCommit))
				{
					files = commitsFilesForCommit
						.Select(f => new CommitFile(f)).ToList();
					commitsFiles[commitId] = files;
					return files;
				}

				Log.Warn($"Failed to get files for {commitId}");
				return Enumerable.Empty<CommitFile>();
			}

			return files;
		}
	}
}