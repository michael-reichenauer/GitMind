﻿using System.Collections.Concurrent;
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
		private readonly IGitService gitService = new GitService();
		private readonly ConcurrentDictionary<string, IList<CommitFile>> commitsFiles =
			new ConcurrentDictionary<string, IList<CommitFile>>();
		private Task currentTask = Task.FromResult(true);
		private string nextIdToGet;

		public static CommitFile[] InProgress =
			{ new CommitFile("      Retrieving files, please retry in a while ... ", "") };
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

					return InProgress;
				}

				return files;
			}
		}


		public async Task<IEnumerable<CommitFile>> GetAsync(string commitId)
		{
			IList<CommitFile> files;
			if (!commitsFiles.TryGetValue(commitId, out files))
			{
				nextIdToGet = commitId;
				await currentTask;
				if (nextIdToGet != commitId)
				{
					// This commit id is no longer relevant 
					return Enumerable.Empty<CommitFile>();
				}

				Task<R<GitCommitFiles>> commitsFilesForCommitTask = 
					gitService.GetCommitsFilesForCommitAsync(null, commitId);
				currentTask = commitsFilesForCommitTask;
				var commitsFilesForCommit = await commitsFilesForCommitTask;

				if (commitsFilesForCommit.HasValue)
				{
					files = commitsFilesForCommit.Value.Files.Select(f => new CommitFile(f.File, "")).ToList();
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