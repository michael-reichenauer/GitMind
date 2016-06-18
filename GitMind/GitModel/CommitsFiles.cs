using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GitMind.Utils;


namespace GitMind.GitModel
{
	internal class CommitsFiles
	{
		private readonly ConcurrentDictionary<string, IList<CommitFile>> commitsFiles =
			new ConcurrentDictionary<string, IList<CommitFile>>();

		public static CommitFile[] InProgress =
			{ new CommitFile("      Retrieving files, please retry in a while ... ", "M") };
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
	}
}