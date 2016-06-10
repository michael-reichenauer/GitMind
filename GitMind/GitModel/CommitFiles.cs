using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.GitModel
{
	internal class CommitFiles
	{
		private readonly object syncRoot = new object();
		private IDictionary<string, IEnumerable<CommitFile>> filesById;

		public CommitFiles(Task<IDictionary<string, IEnumerable<CommitFile>>> commitsFilesTask)
		{
			commitsFilesTask.ContinueWith(t =>
			{
				try
				{
					lock (syncRoot)
					{
						filesById = t.Result;
					}
				}
				catch (Exception e)
				{
					Log.Error($"Failed to get all commits files {e}");
				}
			})
			.RunInBackground();
		}


		public IEnumerable<CommitFile> this[string commitId] 
		{
			get
			{
				IDictionary<string, IEnumerable<CommitFile>> byId;
				lock (syncRoot)
				{
					byId = filesById;
				}

				IEnumerable<CommitFile> commitFiles;
				if (byId == null || !byId.TryGetValue(commitId, out commitFiles))
				{
					return Enumerable.Empty<CommitFile>();
				}

				return commitFiles;
			}
		}
	}
}