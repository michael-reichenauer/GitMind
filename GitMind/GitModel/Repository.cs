using System;
using System.Collections.Generic;
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
			DateTime time,
			Lazy<IReadOnlyKeyedList<string, Branch>> branches, 
			Lazy<IReadOnlyKeyedList<string, Commit>> commits,
			Lazy<Branch> currentBranch,
			Lazy<Commit> currentCommit,
			CommitsFiles commitsFiles)
		{
			Time = time;
			CommitsFiles = commitsFiles;
			this.branches = branches;
			this.commits = commits;
			this.currentBranch = currentBranch;
			this.currentCommit = currentCommit;
		}

		public IReadOnlyKeyedList<string, Branch> Branches => branches.Value;
		public IReadOnlyKeyedList<string, Commit> Commits => commits.Value;
		public Branch CurrentBranch => currentBranch.Value;
		public Commit CurrentCommit => currentCommit.Value;
		public DateTime Time { get; }
		public CommitsFiles CommitsFiles { get; }
	}
}