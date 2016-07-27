using System;
using GitMind.GitModel.Private;
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
			MRepository mRepository,
			Lazy<IReadOnlyKeyedList<string, Branch>> branches,
			Lazy<IReadOnlyKeyedList<string, Commit>> commits,
			Lazy<Branch> currentBranch,
			Lazy<Commit> currentCommit,
			CommitsFiles commitsFiles,
			Status status)
		{
			MRepository = mRepository;
			CommitsFiles = commitsFiles;
			Status = status;
			this.branches = branches;
			this.commits = commits;
			this.currentBranch = currentBranch;
			this.currentCommit = currentCommit;
		}

		public IReadOnlyKeyedList<string, Branch> Branches => branches.Value;
		public IReadOnlyKeyedList<string, Commit> Commits => commits.Value;
		public Branch CurrentBranch => currentBranch.Value;
		public Commit CurrentCommit => currentCommit.Value;
		public MRepository MRepository { get; }
		public CommitsFiles CommitsFiles { get; }
		public Status Status { get; }
	}
}