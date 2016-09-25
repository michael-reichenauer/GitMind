using System.Collections.Generic;


namespace GitMind.GitModel
{
	public class CommitFiles
	{
		public string Id { get; set; }
		public List<CommitFile> Files { get; set; }


		public CommitFiles()
		{
		}

		public CommitFiles(string id, List<CommitFile> files)
		{
			Id = id;
			Files = files;
		}
	}
}