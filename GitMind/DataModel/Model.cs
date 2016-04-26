using System;
using System.Collections.Generic;
using GitMind.DataModel.Private;
using GitMind.Git;


namespace GitMind.DataModel
{
	internal class Model
	{
		private readonly Func<string, Commit> getCommitFunc;

		public static readonly Model None = new Model(
			new IBranch[0],
			new Commit[0],
			_ => Commit.None,
			new Merge[0],
			Commit.None,
			"",
			new string[0],
			null);


		public Model(
			IReadOnlyList<IBranch> branches,
			IReadOnlyList<Commit> commits, 
			Func<string, Commit> getCommitFunc, 
			IReadOnlyList<Merge> merges,
			Commit currentCommit, 
			string currentBranchName, 
			IReadOnlyList<string> allBranchNames, 
			IGitRepo gitRepo)
		{
			this.getCommitFunc = getCommitFunc;
			Branches = branches;
			Commits = commits;
			Merges = merges;
			AllBranchNames = allBranchNames;
			GitRepo = gitRepo;
			CurrentCommit = currentCommit;
			CurrentBranchName = currentBranchName;
		}


		public IReadOnlyList<IBranch> Branches { get; }
		public IReadOnlyList<Commit> Commits { get; }
		public IReadOnlyList<Merge> Merges { get; }
		public IReadOnlyList<string> AllBranchNames { get; }
		public IGitRepo GitRepo { get; }
		public Commit CurrentCommit { get; }
		public string CurrentBranchName { get; }

		public Commit GetCommit(string id) => getCommitFunc(id);
	}
}