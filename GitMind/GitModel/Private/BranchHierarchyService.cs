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

			SetBranchHierarchy(repository.Branches);
		}


		private static void SetParentCommitId(IReadOnlyList<MSubBranch> subBranches)
		{
			foreach (MSubBranch subBranch in subBranches)
			{
				MCommit LatestCommit = subBranch.LatestCommit;

				IEnumerable<MCommit> commits = subBranch.LatestCommit.FirstAncestors()
					.TakeWhile(c => c.BranchXName == subBranch.Name);

				MCommit firstCommit = commits.Any() ? commits.Last() : LatestCommit;				
				
				subBranch.FirstCommitId = firstCommit.Id;
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

					//branch.SubBranchIds.AddRange(GetSubBranchIds(groupByBranch));
					//branch.SubBranches.ForEach(b => b.BranchId = branch.Id);

					branch.CommitIds.AddRange(GetCommitIdsInBranch(groupByBranch));

					branch.Commits.ForEach(c => c.BranchId = branch.Id);

					branch.LatestCommitId = branch.Commits.Any()
						? branch.Commits.First().Id
						: branch.ParentCommitId;

					branch.FirstCommitId = branch.Commits.Any()
						? branch.Commits.Last().Id
						: branch.ParentCommitId;

					branch.Repository.Branches.Add(branch);
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


		private static IEnumerable<string> GetSubBranchIds(IGrouping<string, MSubBranch> groupByBranch)
		{
			return groupByBranch.Select(b => b.SubBranchId);
		}


		private static MBranch ToBranch(MSubBranch subBranch)
		{
			return new MBranch
			{
				Repository = subBranch.Repository,
				Name = subBranch.Name,
				IsMultiBranch = subBranch.IsMultiBranch,
				IsActive = subBranch.IsActive,
				IsAnonymous = subBranch.IsAnonymous,
				ParentCommitId = subBranch.ParentCommitId
			};
		}


		private static void SetBranchHierarchy(IReadOnlyList<MBranch> branches)
		{
			foreach (MBranch branch in branches)
			{
				if (branch.ParentCommitId != null
					&& branch.ParentCommit.BranchId != branch.Id
					&& branch.ParentCommit.BranchId != null)
				{
					branch.ParentBranchId = branch.ParentCommit.BranchId;

					MBranch parentBranch = branch.ParentBranch;
					if (!parentBranch.ChildBranches.Contains(branch))
					{
						parentBranch.ChildBrancheIds.Add(branch.Id);
					}
				}
				else
				{
					Log.Debug($"Branch {branch} has no parent branch");
				}
			}

			//foreach (MBranch xBranch in branches.Where(b => b.ParentBranchId == null))
			//{
			//	LogBranchHierarchy(xBranch, 0);
			//}
		}


		private static void LogBranchHierarchy(MBranch mBranch, int indent)
		{
			string indentText = new string(' ', indent);
			Log.Debug($"{indentText}{mBranch}");

			foreach (MBranch childBranch in mBranch.ChildBranches.OrderBy(b => b.Name))
			{
				LogBranchHierarchy(childBranch, indent + 3);
			}
		}
	}
}