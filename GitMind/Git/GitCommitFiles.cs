using System.Collections.Generic;
using GitMind.Git.Private;


namespace GitMind.Git
{
	internal class GitCommitFiles
	{
		public GitCommitFiles(string id, IReadOnlyList<GitFile> files)
		{
			Id = id;
			Files = files;
		}

		public string Id { get; set; }
		public IReadOnlyList<GitFile> Files { get; set; }
	}
}