using System.Collections.Generic;
using System.Linq;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class BranchHierarchyService : IBranchHierarchyService
	{
		public void SetBranchHierarchy(IReadOnlyList<MSubBranch> subBranches, MRepository repository)
		{
			SetParentCommitId(subBranches);

			GroupSubBranches(subBranches);

			SetBranchHierarchy(repository);
		}


		private static void SetParentCommitId(IReadOnlyList<MSubBranch> subBranches)
		{
			foreach (MSubBranch subBranch in subBranches)
			{
				MCommit LatestCommit = subBranch.LatestCommit;

				IEnumerable<MCommit> commits = subBranch.LatestCommit.FirstAncestors()
					.TakeWhile(c => c.BranchName == subBranch.Name);

				MCommit firstCommit = commits.Any() ? commits.Last() : LatestCommit;				
				
				subBranch.ParentCommitId = firstCommit.FirstParentId;
			}
		}


		private static void GroupSubBranches(IReadOnlyList<MSubBranch> branches)
		{
			var groupByBranchNames = branches.GroupBy(b => b.Name);

			foreach (var groupByBranchName in groupByBranchNames)
			{
				var groupedByParentCommitIds = groupByBranchName.GroupBy(b => b.ParentCommitId);

				foreach (var groupByBranch in groupedByParentCommitIds)
				{
					MSubBranch subBranch = groupByBranch.First();
					MBranch branch = ToBranch(subBranch);

					branch.Id = subBranch.Name + "-" + subBranch.ParentCommitId;

					groupByBranch.ForEach(b => b.BranchId = branch.Id);

					branch.CommitIds.AddRange(GetCommitIdsInBranch(groupByBranch));

					branch.Commits.ForEach(c => c.BranchId = branch.Id);

					branch.LatestCommitId = branch.Commits.Any()
						? branch.Commits.First().Id
						: branch.ParentCommitId;

					branch.FirstCommitId = branch.Commits.Any()
						? branch.Commits.Last().Id
						: branch.ParentCommitId;

					branch.Repository.Branches[branch.Id] = branch;
				}
			}
		}


		private static IEnumerable<string> GetCommitIdsInBranch(IGrouping<string, MSubBranch> groupByBranch)
		{
			return groupByBranch
				.SelectMany(sb =>
					new[] { sb.LatestCommit }
						.Where(c => c.SubBranchId == sb.SubBranchId && c.Id != sb.ParentCommitId)
						.Concat(
							sb.LatestCommit
								.FirstAncestors()
								.TakeWhile(c => c.SubBranchId == sb.SubBranchId && c.Id != sb.ParentCommitId)))
				.Distinct()
				.OrderByDescending(c => c.CommitDate)
				.Select(c => c.Id);
		}


		private static MBranch ToBranch(MSubBranch subBranch)
		{
			return new MBranch
			{
				Repository = subBranch.Repository,
				Name = subBranch.Name,
				IsMultiBranch = subBranch.IsMultiBranch,
				IsActive = subBranch.IsActive,
				ParentCommitId = subBranch.ParentCommitId
			};
		}


		private static void SetBranchHierarchy(MRepository repository)
		{
			foreach (var branch in repository.Branches)
			{
				if (branch.Value.ParentCommitId != null
					&& branch.Value.ParentCommit.BranchId != branch.Value.Id
					&& branch.Value.ParentCommit.BranchId != null)
				{
					branch.Value.ParentBranchId = branch.Value.ParentCommit.BranchId;

					if (branch.Value.ParentBranch.IsMultiBranch 
						&& branch.Value.ParentCommitId == branch.Value.ParentBranch.LatestCommitId)
					{
						branch.Value.ParentBranch.ChildBranchNames.Add(branch.Value.Name);
					}
				}
				else
				{
					Log.Debug($"Branch {branch} has no parent branch");
				}
			}
		}
	}
}