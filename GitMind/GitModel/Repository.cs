using System;
using GitMind.Utils;


namespace GitMind.GitModel
{
	internal class Repository
	{
		private readonly Lazy<IReadOnlyKeyedList<string, Branch>> branches;
		private readonly Lazy<IReadOnlyKeyedList<string, Commit>> commits;
		private readonly Lazy<Branch> currentBranch;
		private readonly Lazy<Commit> currentCommit;


		public Repository(
			Lazy<IReadOnlyKeyedList<string, Branch>> branches, 
			Lazy<IReadOnlyKeyedList<string, Commit>> commits,
			Lazy<Branch> currentBranch,
			Lazy<Commit> currentCommit)
		{
			this.branches = branches;
			this.commits = commits;
			this.currentBranch = currentBranch;
			this.currentCommit = currentCommit;
		}

		public IReadOnlyKeyedList<string, Branch> Branches => branches.Value;
		public IReadOnlyKeyedList<string, Commit> Commits => commits.Value;
		public Branch CurrentBranch => currentBranch.Value;
		public Commit CurrentCommit => currentCommit.Value;
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