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
		}


		private static void SetParentCommitId(MRepository repository)
		{
			foreach (var subBranch in repository.SubBranches)
			{
				MCommit LatestCommit = subBranch.Value.TipCommit;
				if (LatestCommit.BranchId != null)
				{
					if (LatestCommit.BranchName == subBranch.Value.Name)
					{
						subBranch.Value.ParentCommitId = repository.Branches[LatestCommit.BranchId].ParentCommitId;
					}
					else
					{
						// This is a branch with no commits
						subBranch.Value.ParentCommitId = LatestCommit.Id;
					}
				}
				else
				{
					IEnumerable<MCommit> commits = subBranch.Value.TipCommit.CommitAndFirstAncestors()
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
						if (currentcommit != null)
						{
							// Sub branch has at least one commit
							MCommit firstCommit = currentcommit;

							subBranch.Value.ParentCommitId = firstCommit.FirstParentId;
						}
						else
						{					
							// Sub branch has no commits of its own, setting parent commit to same as branch tip
							subBranch.Value.ParentCommitId = LatestCommit.Id;
						}						
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

					var activeTip = groupByBranch
						.Where(b => b.Value.IsActive)
						.OrderByDescending(b => b.Value.TipCommit.CommitDate)
						.FirstOrDefault();
					if (branch.TipCommitId == null && activeTip.Value != null)
					{
						branch.TipCommitId = activeTip.Value.TipCommitId;
					}

					var activeSubBranches = groupByBranch.Where(b => b.Value.IsActive).ToList();
					branch.IsActive = activeSubBranches.Any();
					branch.IsLocal = activeSubBranches.Any(b =>  b.Value.IsLocal);
					branch.IsRemote = activeSubBranches.Any(b => b.Value.IsRemote);

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
					branch.TipCommitId = commits.Any() ? commits.First().Id : branch.ParentCommitId;

					branch.FirstCommitId = commits.Any() ? commits.Last().Id : branch.ParentCommitId;
					branch.CommitIds = commits.Select(c => c.Id).ToList();
				}

				if (!branch.CommitIds.Any())
				{
					if (branch.TipCommitId != null)
					{
						// Active Branch has no commits of its own
						branch.FirstCommitId = branch.TipCommitId;
					}
					else
					{
						// Branch has no commits of its own
						branch.TipCommitId = branch.ParentCommitId;
						branch.FirstCommitId = branch.ParentCommitId;
					}
				}
			}


			foreach (MBranch branch in repository.Branches.Values.Where(b => !b.Commits.Any()))
			{
				MCommit commit = new MCommit();
				commit.IsVirtual = true;
				commit.Repository = repository;
				commit.BranchId = branch.Id;
				commit.BranchName = branch.Name;
				CopyToCommit(branch, commit);
				SetChildOfParents(commit);
				repository.Commits[commit.Id] = commit;

				branch.CommitIds.Add(commit.Id);
				branch.TipCommitId = commit.Id;
				branch.FirstCommitId = commit.Id;

				branch.TipCommit.BranchTips = $"Tip of branch {branch.Name} at: ";
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
						&& branch.Value.ParentCommitId == branch.Value.ParentBranch.TipCommitId
						&& !branch.Value.ParentBranch.ChildBranchNames.Contains(branch.Value.Name))
					{
						branch.Value.ParentBranch.ChildBranchNames.Add(branch.Value.Name);
					}
				}
			}
		}


		private static void CopyToCommit(MBranch branch, MCommit commit)
		{
			commit.Id = GetId();
			commit.CommitId = branch.ParentCommit.CommitId;
			commit.ShortId = commit.CommitId.Substring(0, 6);
			commit.Subject = branch.ParentCommit.Subject;
			commit.Author = branch.ParentCommit.Author;
			commit.AuthorDate = branch.ParentCommit.AuthorDate;
			commit.CommitDate = branch.ParentCommit.CommitDate + TimeSpan.FromSeconds(1);
			commit.Tickets = branch.ParentCommit.Tickets;
			commit.ParentIds = new List<string> { branch.ParentCommitId };
		}

		private static string GetId()
		{
			string id =
				Guid.NewGuid().ToString().Replace("-", "")
				+ Guid.NewGuid().ToString().Replace("-", "");
			return id.Substring(0, 40);
		}


		private static void SetChildOfParents(MCommit commit)
		{
			bool isFirstParent = true;
			foreach (string parentId in commit.ParentIds)
			{
				IList<string> childIds = commit.Repository.ChildIds(parentId);
				if (!childIds.Contains(commit.Id))
				{
					childIds.Add(commit.Id);
				}

				if (isFirstParent)
				{
					isFirstParent = false;
					IList<string> firstChildIds = commit.Repository.FirstChildIds(parentId);
					if (!firstChildIds.Contains(commit.Id))
					{
						firstChildIds.Add(commit.Id);
					}
				}
			}
		}
	}
}