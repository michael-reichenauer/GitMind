using System.Collections.Generic;
using System.Linq;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class BranchHierarchyService : IBranchHierarchyService
	{
		public void SetBranchHierarchy(MRepository repository)
		{
			Timing t = new Timing();
			SetParentCommitId(repository);
			t.Log("SetParentCommitId");

			GroupSubBranches(repository);
			t.Log("GroupSubBranches");

			SetBranchHierarchyImpl(repository);
			t.Log("SetBranchHierarchyImpl");
		}


		private static void SetParentCommitId(MRepository repository)
		{
			foreach (var subBranch in repository.SubBranches)
			{
				MCommit LatestCommit = subBranch.Value.LatestCommit;
				if (LatestCommit.BranchId != null)
				{
					subBranch.Value.ParentCommitId = repository.Branches[LatestCommit.BranchId].ParentCommitId;
				}
				else
				{
					IEnumerable<MCommit> commits = subBranch.Value.LatestCommit.FirstAncestors()
						.TakeWhile(c => c.BranchName == subBranch.Value.Name);

					MCommit currentcommit = null;
					foreach (MCommit commit in commits)
					{
						if (commit.BranchId != null)
						{
							subBranch.Value.ParentCommitId = repository.Branches[commit.BranchId].ParentCommitId;
							break;
						}
						currentcommit = commit;
					}

					if (subBranch.Value.ParentCommitId == null)
					{
						MCommit firstCommit = currentcommit ?? LatestCommit;

						subBranch.Value.ParentCommitId = firstCommit.FirstParentId;
					}
				}
			}
		}


		private static void GroupSubBranches(MRepository repository)
		{
			Timing t = new Timing();
			var groupByBranchNames = repository.SubBranches.GroupBy(b => b.Value.Name);

			foreach (var groupByBranchName in groupByBranchNames)
			{
				var groupedByParentCommitIds = groupByBranchName.GroupBy(b => b.Value.ParentCommitId);

				foreach (IGrouping<string, KeyValuePair<string, MSubBranch>> groupByBranch in groupedByParentCommitIds)
				{
					MSubBranch subBranch = groupByBranch.First().Value;
					MBranch branch = ToBranch(subBranch);

					if (subBranch.ParentCommitId == null)
					{
						
					}
					branch.Id = subBranch.Name + "-" + subBranch.ParentCommitId;

					groupByBranch.ForEach(b => b.Value.BranchId = branch.Id);

					//branch.CommitIds.AddRange(GetCommitIdsInBranch(groupByBranch));

					//branch.Commits.ForEach(c => c.BranchId = branch.Id);

					//branch.LatestCommitId = branch.Commits.Any()
					//	? branch.Commits.First().Id
					//	: branch.ParentCommitId;

					//branch.FirstCommitId = branch.Commits.Any()
					//	? branch.Commits.Last().Id
					//	: branch.ParentCommitId;

					branch.Repository.Branches[branch.Id] = branch;
				}
			}
		
			t.Log("Grouped branches");

			foreach (var commit in repository.Commits)
			{
				if (commit.Value.BranchId == null)
				{
					string subBranchId = commit.Value.SubBranchId;
					string branchId = repository.SubBranches[subBranchId].BranchId;
					commit.Value.BranchId = branchId;
					repository.Branches[branchId].CommitIds.Add(commit.Value.Id);
				}
				else
				{
					repository.Branches[commit.Value.BranchId].CommitIds.Add(commit.Value.Id);
				}
			}

			t.Log("Added new commits");

			foreach (var branch in repository.Branches.Values)
			{
				List<MCommit> commits = branch.Commits.OrderByDescending(b => b.CommitDate).ToList();
				branch.LatestCommitId = commits.Any() ? commits.First().Id : branch.ParentCommitId;

				branch.FirstCommitId = commits.Any() ? commits.Last().Id : branch.ParentCommitId;
				branch.CommitIds = commits.Select(c => c.Id).ToList();
			}

			t.Log("Sorted and found first and latest commits");
		}


		//private static IEnumerable<string> GetCommitIdsInBranch(
		//	IGrouping<string, KeyValuePair<string, MSubBranch>> groupByBranch)
		//{
		//	return groupByBranch
		//		.SelectMany(sb =>
		//			new[] { sb.Value.LatestCommit }
		//				.Where(c => c.SubBranchId == sb.Value.SubBranchId && c.Id != sb.Value.ParentCommitId)
		//				.Concat(
		//					sb.Value.LatestCommit
		//						.FirstAncestors()
		//						.TakeWhile(c => c.SubBranchId == sb.Value.SubBranchId && c.Id != sb.Value.ParentCommitId)))
		//		.Distinct()
		//		.OrderByDescending(c => c.CommitDate)
		//		.Select(c => c.Id);
		//}


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


		private static void SetBranchHierarchyImpl(MRepository repository)
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