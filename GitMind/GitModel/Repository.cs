using System;
using System.Collections.Generic;
using System.Windows.Forms.VisualStyles;
using GitMind.Common;
using GitMind.Features.StatusHandling;
using GitMind.GitModel.Private;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind.GitModel
{
	internal class Repository
	{
		private readonly Lazy<IReadOnlyKeyedList<string, Branch>> branches;
		private readonly Lazy<IReadOnlyDictionary<CommitId, Commit>> commits;
		private readonly Lazy<Branch> currentBranch;
		private readonly Lazy<Commit> currentCommit;
		private readonly CommitId rootId;
		private readonly CommitId unComittedId;


		public Repository(
			MRepository mRepository,
			Lazy<IReadOnlyKeyedList<string, Branch>> branches,
			Lazy<IReadOnlyDictionary<CommitId, Commit>> commits,
			Lazy<Branch> currentBranch,
			Lazy<Commit> currentCommit,
			ICommitsFiles commitsFiles,
			GitStatus2 status,
			CommitId rootId,
			CommitId unComittedId)
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
		public IReadOnlyDictionary<CommitId, Commit> Commits => commits.Value;
		public Branch CurrentBranch => currentBranch.Value;
		public Commit CurrentCommit => currentCommit.Value;
		public MRepository MRepository { get; }
		public ICommitsFiles CommitsFiles { get; }
		public GitStatus2 Status { get; }
		public Commit RootCommit => commits.Value[rootId];
		public Commit UnComitted => unComittedId == CommitId.Uncommitted ? commits.Value[unComittedId] : null;
	}
}