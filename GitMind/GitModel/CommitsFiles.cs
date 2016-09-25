using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;


namespace GitMind.GitModel
{
	internal class CommitsFiles
	{
		private readonly IGitCommitsService gitCommitsService = new GitCommitsService();
		private readonly ConcurrentDictionary<string, IList<CommitFile>> commitsFiles =
			new ConcurrentDictionary<string, IList<CommitFile>>();
		private Task currentTask = Task.FromResult(true);
		private string nextIdToGet;


		private static readonly List<CommitFile> EmptyFileList = Enumerable.Empty<CommitFile>().ToList();

		public int Count => commitsFiles.Count;

		public bool Add(CommitFiles commitFiles)
		{
			if (commitsFiles.ContainsKey(commitFiles.Id))
			{
				return false;
			}

			commitsFiles[commitFiles.Id] = commitFiles.Files ?? EmptyFileList;
			return true;
		}



		public IEnumerable<CommitFile> this[string commitId]
		{
			get
			{
				IList<CommitFile> files;
				if (!commitsFiles.TryGetValue(commitId, out files))
				{
					Log.Warn($"Commit {commitId} not cached");

					return Enumerable.Empty<CommitFile>();
				}

				return files;
			}
		}


		public async Task<IEnumerable<CommitFile>> GetAsync(
			string gitRepositoryPath, string commitId)
		{
			IList<CommitFile> files;
			if (commitId == Commit.UncommittedId || !commitsFiles.TryGetValue(commitId, out files))
			{
				nextIdToGet = commitId;
				await currentTask;
				if (nextIdToGet != commitId)
				{
					// This commit id is no longer relevant 
					return Enumerable.Empty<CommitFile>();
				}

				Task<R<GitCommitFiles>> commitsFilesForCommitTask =
					gitCommitsService.GetFilesForCommitAsync(gitRepositoryPath, commitId);
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