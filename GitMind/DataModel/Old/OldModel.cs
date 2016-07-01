using System;
using System.Collections.Generic;
using GitMind.Git;


namespace GitMind.DataModel.Old
{
	internal class OldModel
	{
		private readonly Func<string, OldCommit> getCommitFunc;

		public static readonly OldModel None = new OldModel(
			new IBranch[0],
			new OldCommit[0],
			_ => OldCommit.None,
			new OldMerge[0],
			OldCommit.None,
			"",
			new string[0],
			null);


		public OldModel(
			IReadOnlyList<IBranch> branches,
			IReadOnlyList<OldCommit> commits, 
			Func<string, OldCommit> getCommitFunc, 
			IReadOnlyList<OldMerge> merges,
			OldCommit currentCommit, 
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
		public IReadOnlyList<OldCommit> Commits { get; }
		public IReadOnlyList<OldMerge> Merges { get; }
		public IReadOnlyList<string> AllBranchNames { get; }
		public IGitRepo GitRepo { get; }
		public OldCommit CurrentCommit { get; }
		public string CurrentBranchName { get; }

		public OldCommit GetCommit(string id) => getCommitFunc(id);
	}
}