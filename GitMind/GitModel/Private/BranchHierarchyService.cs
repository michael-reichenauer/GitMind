using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class BranchHierarchyService : IBranchHierarchyService
	{
		public void SetBranchHierarchy(MRepository repository)
		{
			SetParentCommitId(repository);

			GroupSubBranches(repository);

			SetBranchHierarchyImpl(repository);

			repository.SubBranches.Clear();
		}


		private static void SetParentCommitId(MRepository repository)
		{
			foreach (var subBranch in repository.SubBranches)
			{
				MCommit LatestCommit = subBranch.Value.LatestCommit;
				if (LatestCommit.BranchId != null)
				{
					if (LatestCommit.BranchName == subBranch.Value.Name)
					{
						subBranch.Value.ParentCommitId = repository.Branches[LatestCommit.BranchId].ParentCommitId;
					}
					else
					{
						// This is a branch with no commits
						MCommit firstCommit = LatestCommit;
						subBranch.Value.ParentCommitId = firstCommit.FirstParentId;
					}
				}
				else
				{
					IEnumerable<MCommit> commits = subBranch.Value.LatestCommit.FirstAncestors()
						.TakeWhile(c => c.BranchName == subBranch.Value.Name);

					bool foundParent = false;
					MCommit currentcommit = null;
					foreach (MCommit commit in commits)
					{
						if (commit.BranchId != null)
						{
							subBranch.Value.ParentCommitId = repository.Branches[commit.BranchId].ParentCommitId;
							foundParent = true;
							break;
						}
						currentcommit = commit;
					}

					if (!foundParent)
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

					string branchId = subBranch.Name + "-" + subBranch.ParentCommitId;

					MBranch branch;
					if (!repository.Branches.TryGetValue(branchId, out branch))
					{
						branch = ToBranch(subBranch);
						branch.Id = branchId;
						branch.Repository.Branches[branch.Id] = branch;
					}

					groupByBranch.ForEach(b => b.Value.BranchId = branch.Id);		
				}
			}

			foreach (var commit in repository.Commits)
			{
				if (commit.Value.BranchId == null)
				{
					string subBranchId = commit.Value.SubBranchId;
					string branchId = repository.SubBranches[subBranchId].BranchId;
					commit.Value.BranchId = branchId;
					commit.Value.SubBranchId = null;
					repository.Branches[branchId].TempCommitIds.Add(commit.Value.Id);
				}
			}

			foreach (var branch in repository.Branches.Values)
			{
				if (branch.TempCommitIds.Any())
				{
					branch.CommitIds.AddRange(branch.TempCommitIds);
					branch.TempCommitIds.Clear();

					List<MCommit> commits = branch.Commits.OrderByDescending(b => b.CommitDate).ToList();
					branch.LatestCommitId = commits.Any() ? commits.First().Id : branch.ParentCommitId;

					branch.FirstCommitId = commits.Any() ? commits.Last().Id : branch.ParentCommitId;
					branch.CommitIds = commits.Select(c => c.Id).ToList();
				}

				if (!branch.CommitIds.Any())
				{
					// Branch has no commits of its own
					branch.LatestCommitId = branch.ParentCommitId;
					branch.FirstCommitId = branch.ParentCommitId;
				}
			}


			foreach (MBranch branch in repository.Branches.Values.Where(b => !b.Commits.Any()))
			{
				string branchTipText = $"({branch.Name}) ";
				if (branch.LatestCommit.BranchTips != null 
					&& -1 == branch.LatestCommit.BranchTips.IndexOf(branch.Name, StringComparison.Ordinal))
				{
					branch.LatestCommit.BranchTips += branchTipText;
				}
				else
				{
					branch.LatestCommit.BranchTips = branchTipText;
				}
			}
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
			}
		}
	}
}