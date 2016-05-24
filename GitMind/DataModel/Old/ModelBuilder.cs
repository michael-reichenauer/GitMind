using System.Collections.Generic;
using GitMind.Git;


namespace GitMind.DataModel.Old
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

		public List<OldMerge> Merges { get; } = new List<OldMerge>();
		public IGitRepo GitRepo { get; }

		public List<BranchBuilder> ActiveBranches { get; } = new List<BranchBuilder>();
		public List<BranchBuilder> AllBranches { get; } = new List<BranchBuilder>();


		public bool IsParentBranch(BranchBuilder parent, BranchBuilder child)
		{
			return -1 == branchPriority.Compare(parent, child);
		}
	}
}
