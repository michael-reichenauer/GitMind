using System.Collections.Generic;
using GitMind.Git;


namespace GitMind.DataModel.Private
{
	internal class ModelBuilder
	{
		private readonly BranchPriority branchPriority;


		public ModelBuilder(
			IGitRepo gitRepo,
			BranchPriority branchPriority)
		{
			this.branchPriority = branchPriority;
			GitRepo = gitRepo;
			Commits = new Commits(gitRepo);
		
		}


		public Commits Commits { get; }

		public List<Merge> Merges { get; } = new List<Merge>();
		public IGitRepo GitRepo { get; }

		public List<BranchBuilder> ActiveBranches { get; } = new List<BranchBuilder>();
		public List<BranchBuilder> AllBranches { get; } = new List<BranchBuilder>();


		public bool IsParentBranch(BranchBuilder parent, BranchBuilder child)
		{
			return -1 == branchPriority.Compare(parent, child);
		}
	}
}
