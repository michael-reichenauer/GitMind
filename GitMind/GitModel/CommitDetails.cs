using System.Collections.Generic;


namespace GitMind.GitModel
{
	internal class CommitDetails
	{
		public IReadOnlyList<CommitFile> Files { get; }
		public string Message { get; }


		public CommitDetails(IReadOnlyList<CommitFile> files, string message)
		{
			Files = files;
			Message = message;
		}
	}
}