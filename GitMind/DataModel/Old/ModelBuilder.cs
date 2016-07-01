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

		public List<OldBranchBuilder> ActiveBranches { get; } = new List<OldBranchBuilder>();
		public List<OldBranchBuilder> AllBranches { get; } = new List<OldBranchBuilder>();


		public bool IsParentBranch(OldBranchBuilder parent, OldBranchBuilder child)
		{
			return -1 == branchPriority.Compare(parent, child);
		}
	}
}
