using GitMind.Features.StatusHandling;
using GitMind.Git;
using GitMind.Utils.Git;


namespace GitMind.GitModel
{
	public class CommitFile
	{
		private readonly GitFile2 gitFile;


		public CommitFile(GitFile2 gitFile)
		{
			this.gitFile = gitFile;
		}


		public string Path => gitFile.FilePath;
		public string OldPath => gitFile.OldFilePath;

		public GitConflict Conflict => new GitConflict(Path, null, null, null);

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
			else if (Status.HasFlag(GitFileStatus.Conflict) && 
				(Conflict.IsOursDeleted || Conflict.IsTheirsDeleted))
			{
				return "CD";
			}
			else if (Status.HasFlag(GitFileStatus.Conflict))
			{
				return "C";
			}

			return "";
		}
	
	}
}