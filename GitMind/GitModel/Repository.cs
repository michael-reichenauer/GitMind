using System;
using System.Collections.Generic;
using GitMind.Features.StatusHandling;
using GitMind.GitModel.Private;
using GitMind.Utils;


namespace GitMind.GitModel
{
	internal class Repository
	{
		private readonly Lazy<IReadOnlyKeyedList<string, Branch>> branches;
		private readonly Lazy<IReadOnlyList<Commit>> commits;
		private readonly Lazy<Branch> currentBranch;
		private readonly Lazy<Commit> currentCommit;
		private readonly int rootId;
		private readonly int unComittedId;


		public Repository(
			MRepository mRepository,
			Lazy<IReadOnlyKeyedList<string, Branch>> branches,
			Lazy<IReadOnlyList<Commit>> commits,
			Lazy<Branch> currentBranch,
			Lazy<Commit> currentCommit,
			ICommitsFiles commitsFiles,
			Status status,
			int rootId,
			int unComittedId)
		{
			MRepository = mRepository;
			CommitsFiles = commitsFiles;
			Status = status;

			this.branches = branches;
			this.commits = commits;
			this.currentBranch = currentBranch;
			this.currentCommit = currentCommit;
			this.rootId = rootId;
			this.unComittedId = unComittedId;
		}

		public IReadOnlyKeyedList<string, Branch> Branches => branches.Value;
		public IReadOnlyList<Commit> Commits => commits.Value;
		public Branch CurrentBranch => currentBranch.Value;
		public Commit CurrentCommit => currentCommit.Value;
		public MRepository MRepository { get; }
		public ICommitsFiles CommitsFiles { get; }
		public Status Status { get; }
		public Commit RootCommit => commits.Value[rootId];
		public Commit UnComitted => unComittedId != -1 ? commits.Value[unComittedId] : null;
	}
}