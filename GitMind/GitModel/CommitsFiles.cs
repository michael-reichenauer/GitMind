using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.GitModel
{
	[SingleInstance]
	internal class CommitsFiles : ICommitsFiles
	{
		private readonly IGitCommitsService gitCommitsService;

		private readonly ConcurrentDictionary<string, IList<CommitFile>> commitsFiles =
			new ConcurrentDictionary<string, IList<CommitFile>>();

		private Task currentTask = Task.FromResult(true);
		private string nextIdToGet;


		public CommitsFiles(IGitCommitsService gitCommitsService)
		{
			this.gitCommitsService = gitCommitsService;
		}


		public async Task<IEnumerable<CommitFile>> GetAsync(string commitId)
		{
			if (commitId == Commit.UncommittedId || !commitsFiles.TryGetValue(commitId, out var files))
			{
				nextIdToGet = commitId;
				await currentTask;
				if (nextIdToGet != commitId)
				{
					// This commit id is no longer relevant 
					return Enumerable.Empty<CommitFile>();
				}

				Task<R<GitCommitFiles>> commitsFilesForCommitTask =
					gitCommitsService.GetFilesForCommitAsync(commitId);

				currentTask = commitsFilesForCommitTask;
				var commitsFilesForCommit = await commitsFilesForCommitTask;

				if (commitsFilesForCommit.HasValue)
				{
					files = commitsFilesForCommit.Value.Files
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