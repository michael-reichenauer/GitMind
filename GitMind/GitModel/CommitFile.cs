using GitMind.Git;
using GitMind.Utils.Git;


namespace GitMind.GitModel
{
	public class CommitFile
	{
		private readonly GitFile2 gitFile;


		public CommitFile(GitFile2 gitFile, GitConflictFile conflict = null)
		{
			this.gitFile = gitFile;
			Conflict = conflict ??
				new GitConflictFile(null, gitFile.FilePath, null, null, null, GitFileStatus.Modified);
		}


		public string Path => gitFile.FilePath;
		public string FullFilePath => gitFile.FullFilePath;
		public string OldPath => gitFile.OldFilePath;

		public GitConflictFile Conflict { get; }

		public GitFileStatus Status => gitFile.Status;

		public string StatusText => GetStatusText();


		private string GetStatusText()
		{
			if (Status.HasFlag(GitFileStatus.Renamed) && Status.HasFlag(GitFileStatus.Modified))
			{
				return "RM";
			}
			else if (Status.HasFlag(GitFileStatus.Renamed))
			{
				return "R";
			}
			else if (Status.HasFlag(GitFileStatus.Added))
			{
				return "A";
			}
			else if (Status.HasFlag(GitFileStatus.Deleted))
			{
				return "D";
			}
			else if (Status.HasFlag(GitFileStatus.ConflictDM))
			{
				return "CDM";
			}
			else if (Status.HasFlag(GitFileStatus.ConflictMD))
			{
				return "CMD";
			}
			else if (Status.HasFlag(GitFileStatus.ConflictAA))
			{
				return "CAA";
			}
			else if (Status.HasFlag(GitFileStatus.Conflict))
			{
				return "C";
			}

			return "";
		}

	}
}