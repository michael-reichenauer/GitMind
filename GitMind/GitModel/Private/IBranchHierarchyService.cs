using System.Collections.Generic;


namespace GitMind.GitModel.Private
{
	internal interface IBranchHierarchyService
	{
		void SetBranchHierarchy(IReadOnlyList<MSubBranch> subBranches, MRepository mRepository);
	}
}