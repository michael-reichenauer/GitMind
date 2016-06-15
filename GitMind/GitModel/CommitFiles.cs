using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace GitMind.GitModel
{
	internal class CommitFiles
	{
		private readonly Task<IDictionary<string, IList<CommitFile>>> commitsFilesTask;
		public static CommitFile[] InProgress =
			{ new CommitFile("", "      Retrieving files, please retry in a while ... ", "M") };

		public CommitFiles(Task<IDictionary<string, IList<CommitFile>>> commitsFilesTask)
		{
			this.commitsFilesTask = commitsFilesTask;
		}


		public IEnumerable<CommitFile> this[string commitId]
		{
			get
			{
				if (commitsFilesTask.Status != TaskStatus.RanToCompletion)
				{
					return InProgress;
				}

				IDictionary<string, IList<CommitFile>> byId = commitsFilesTask.Result;

				IList<CommitFile> commitFiles;
				if (byId == null || !byId.TryGetValue(commitId, out commitFiles))
				{
					return Enumerable.Empty<CommitFile>();
				}

				return commitFiles;
			}
		}
	}
}