using System;
using GitMind.Utils;


namespace GitMind.GitModel
{
	internal class Repository
	{
		private readonly Lazy<IReadOnlyKeyedList<string, Branch>> branches;
		private readonly Lazy<IReadOnlyKeyedList<string, Commit>> commits;

		public Repository(
			Lazy<IReadOnlyKeyedList<string, Branch>> branches, 
			Lazy<IReadOnlyKeyedList<string, Commit>> commits)
		{
			this.branches = branches;
			this.commits = commits;
		}


		public IReadOnlyKeyedList<string, Branch> Branches => branches.Value;
		public IReadOnlyKeyedList<string, Commit> Commits => commits.Value;
	}


	internal class Tag
	{
		public string Name { get; set; }
		public string CommitId { get; set; }


		public Tag(string name, string commitId)
		{
			Name = name;
			CommitId = commitId;
		}
	}
}