using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Features.Branching.Private;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class BranchHierarchyService : IBranchHierarchyService
	{
		private readonly IGitBranchService gitBranchService;


		public BranchHierarchyService()
			: this(new GitBranchService())
		{
		}

		public BranchHierarchyService(IGitBranchService gitBranchService)
		{
			this.gitBranchService = gitBranchService;
		}


		public void SetBranchHierarchy(MRepository repository)
		{
			CombineMainWithLocalSubBranches(repository);

			SetParentCommitId(repository);		

			GroupSubBranchesIntoMainBranches(repository);

			MoveCommitsIntoBranches(repository);

			AddEmptyBranchesVirtualTipCommits(repository);

			SetLocalOnlyAhead(repository);

			SetLocalAndRemoteAhead(repository);

			SetBranchHierarchyImpl(repository);
		}


		private void CombineMainWithLocalSubBranches(MRepository repository)
		{
			foreach (MBranch branch in repository.Branches.Values.Where(b => b.IsMainPart).ToList())
			{
				MBranch localPart = repository.Branches[branch.LocalSubBranchId];
				foreach (string commitId in localPart.CommitIds)
				{
					branch.CommitIds.Add(commitId);
					repository.Commits[commitId].BranchId = branch.Id;
				}

				repository.Branches.Remove(localPart.Id);

				branch.IsMainPart = false;
				branch.LocalSubBranchId = null;
			}
		}


		private static void SetParentCommitId(MRepository repository)
		{
			foreach (var subBranch in repository.SubBranches.Values)
			{
				MCommit tipCommit = subBranch.TipCommit;
				if (tipCommit.BranchId != null)
				{
					if (tipCommit.BranchName == subBranch.Name)
					{
						subBranch.ParentCommitId = repository.Branches[tipCommit.BranchId].ParentCommitId;
					}
					else
					{
						// This is a branch with no commits
						subBranch.ParentCommitId = tipCommit.Id;
					}
				}
				else
				{
					IEnumerable<MCommit> commits = subBranch.TipCommit.CommitAndFirstAncestors()
						.TakeWhile(c => c.BranchName == subBranch.Name);

					bool foundParent = false;
					MCommit currentcommit = null;
					foreach (MCommit commit in commits)
					{
						if (commit.BranchId != null)
						{
							subBranch.ParentCommitId = repository.Branches[commit.BranchId].ParentCommitId;
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

							subBranch.ParentCommitId = firstCommit.FirstParentId;
						}
						else
						{
							// Sub branch has no commits of its own, setting parent commit to same as branch tip
							subBranch.ParentCommitId = tipCommit.Id;
						}
					}
				}
			}
		}


		private void GroupSubBranchesIntoMainBranches(MRepository repository)
		{
			// Group all sub branches by name, i.e. all sub branches in group will have the same name
			var groupByBranchNames = repository.SubBranches.GroupBy(b => b.Value.Name);

			foreach (var groupByBranchName in groupByBranchNames)
			{
				// Group sub branches by parent commit id, i.e. all sub branches in the group will have same
				// parent id and same name
				var groupedByParentCommitIds = groupByBranchName.GroupBy(b => b.Value.ParentCommitId);

				foreach (var groupByBranch in groupedByParentCommitIds)
				{
					// Group all sub branches in the group into one main branch
					GroupSubBranchesIntoOneMainBranch(groupByBranch);
				}
			}
		}


		private static void MoveCommitsIntoBranches(MRepository repository)
		{
			foreach (MCommit commit in repository.Commits.Values)
			{
				if (commit.BranchId == null)
				{
					string subBranchId = commit.SubBranchId;
					string branchId = repository.SubBranches[subBranchId].BranchId;
					commit.BranchId = branchId;
					commit.SubBranchId = null;
					repository.Branches[branchId].TempCommitIds.Add(commit.Id);
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
		}


		private static void GroupSubBranchesIntoOneMainBranch(
			IGrouping<string, KeyValuePair<string, MSubBranch>> groupByBranch)
		{
			// All sub branches in the groupByBranch have same name and parent commit id, lets take
			// the first sub branch and base the branch corresponding to the group on that sub branch
			MSubBranch subBranch = groupByBranch.First().Value;

			string branchId = subBranch.Name + "-" + subBranch.ParentCommitId;

			MBranch branch = GetBranch(branchId, subBranch);

			// Get active sub branches in group (1 if either local or remote, 2 if both)
			var activeSubBranches = groupByBranch
				.Where(b => b.Value.IsActive).Select(g => g.Value)
				.ToList();

			// Get the most resent tip (local or remote)
			var activeTip = activeSubBranches
				.OrderByDescending(b => b.TipCommit.CommitDate)
				.FirstOrDefault();

			if (branch.TipCommitId == null && activeTip != null)
			{
				branch.TipCommitId = activeTip.TipCommitId;
			}

			branch.IsActive = activeSubBranches.Any();

			MSubBranch localSubBranch = activeSubBranches.FirstOrDefault(b => b.IsLocal);
			MSubBranch remoteSubBranch = activeSubBranches.FirstOrDefault(b => b.IsRemote);

			branch.IsLocal = localSubBranch != null;
			branch.LocalTipCommitId = localSubBranch?.TipCommitId;
			branch.IsRemote = remoteSubBranch != null;
			branch.RemoteTipCommitId = remoteSubBranch?.TipCommitId;
			branch.IsCurrent = activeSubBranches.Any(b => b.IsCurrent);
			branch.IsDetached = activeSubBranches.Any(b => b.IsDetached);
			branch.LocalAheadCount = 0;
			branch.RemoteAheadCount = 0;

			// Set branch if of each sub branch
			groupByBranch.ForEach(b => b.Value.BranchId = branch.Id);
		}


		private static MBranch GetBranch(string branchId, MSubBranch subBranch)
		{
			MRepository repository = subBranch.Repository;

			MBranch branch;
			if (!repository.Branches.TryGetValue(branchId, out branch))
			{
				branch = CreateBranchBasedOnSubBranch(subBranch);
				branch.Id = branchId;
				branch.Repository.Branches[branch.Id] = branch;
			}

			return branch;
		}


		private static void AddEmptyBranchesVirtualTipCommits(MRepository repository)
		{
			IEnumerable<MBranch> emptyBranches = repository.Branches.Values
				.Where(b => !b.Commits.Any() && !b.IsLocalPart);

			foreach (MBranch branch in emptyBranches)
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

				branch.TipCommit.BranchTips = $"{branch.Name} branch tip";
			}
		}


		private void SetLocalAndRemoteAhead(MRepository repository)
		{
			foreach (MBranch branch in repository.Branches.Values
				.Where(b => b.IsActive
				            && b.IsLocal
				            && b.IsRemote
				            && b.LocalTipCommitId != b.RemoteTipCommitId).ToList())
			{
				string localTip = branch.LocalTipCommitId;
				string remoteTip = branch.RemoteTipCommitId;

				if (localTip == Commit.UncommittedId)
				{
					localTip = branch.Repository.Commits[branch.LocalTipCommitId].FirstParentId;
				}

				R<GitDivergence> div = gitBranchService.CheckAheadBehind(
					repository.WorkingFolder, localTip, remoteTip);

				if (div.HasValue)
				{
					string commonTip = div.Value.CommonId;

					MCommit commonCommit = repository.Commits[commonTip];
					commonCommit.CommitAndFirstAncestors().ForEach(c => c.IsCommon = true);

					if ((commonTip != localTip)
					    || (branch.LocalTipCommitId == Commit.UncommittedId && commonTip != remoteTip))
					{
						MakeLocalBranch(repository, branch, localTip, commonTip);
					}

					if (branch.IsLocal)
					{
						int localCount = 0;
						Stack<MCommit> commits = new Stack<MCommit>();
						commits.Push(branch.Repository.Commits[localTip]);

						while (commits.Any())
						{
							MCommit commit = commits.Pop();
							if (!commit.IsCommon && commit.Branch == branch)
							{
								commit.IsLocalAhead = true;
								localCount++;
								commit.Parents.Where(p => p.Branch == branch).ForEach(p => commits.Push(p));
							}
						}

						branch.LocalAheadCount = localCount;
					}

					if (branch.IsRemote)
					{
						int remoteCount = 0;
						Stack<MCommit> commits = new Stack<MCommit>();
						commits.Push(branch.Repository.Commits[remoteTip]);

						while (commits.Any())
						{
							MCommit commit = commits.Pop();
							if (!commit.IsCommon && commit.Branch == branch)
							{
								commit.IsRemoteAhead = true;
								remoteCount++;
								commit.Parents.Where(p => p.Branch == branch).ForEach(p => commits.Push(p));
							}
						}

						branch.RemoteAheadCount = remoteCount;
					}
				}
			}
		}


		private static void SetLocalOnlyAhead(MRepository repository)
		{
			// Set local only branches ahead marker
			repository.Branches.Values
				.Where(b => b.IsActive && b.IsLocal && !b.IsRemote)
				.ForEach(b =>
				{
					b.LocalAheadCount = b.Commits.Count(c => !c.IsVirtual);
					b.Commits.Where(c => !c.IsVirtual).ForEach(c => c.IsLocalAhead = true);
				});
		}


		private static void MakeLocalBranch(
			MRepository repository, 
			MBranch branch, 
			string localTip,
			string commonTip)
		{
			string name = $"{branch.Name}";
			string branchId = name + "-" + commonTip;
			MBranch localBranch;
			if (!repository.Branches.TryGetValue(branchId, out localBranch))
			{
				localBranch = new MBranch
				{
					IsLocalPart = true,
					Repository = branch.Repository,
					Name = name,
					IsMultiBranch = false,
					IsActive = true,
					IsLocal = true,
					ParentCommitId = commonTip,
				};

				localBranch.Id = branchId;
				repository.Branches[localBranch.Id] = localBranch;
			}

			localBranch.TipCommitId = localTip;
			localBranch.LocalTipCommitId = localTip;
			localBranch.IsCurrent = branch.IsCurrent;

			Stack<MCommit> commits = new Stack<MCommit>();
			commits.Push(repository.Commits[localTip]);

			while (commits.Any())
			{
				MCommit commit = commits.Pop();
				if (!commit.IsCommon && commit.Branch == branch)
				{
					commit.IsLocalAhead = true;
					localBranch.CommitIds.Add(commit.Id);
					branch.CommitIds.Remove(commit.Id);
					commit.BranchId = localBranch.Id;
					commit.Parents.Where(p => p.Branch == branch).ForEach(p => commits.Push(p));
				}
			}

			localBranch.LocalAheadCount = localBranch.Commits.Count();
			localBranch.RemoteAheadCount = 0;

			if (branch.TipCommitId == Commit.UncommittedId)
			{
				localBranch.TipCommitId = Commit.UncommittedId;
				localBranch.CommitIds.Insert(0, Commit.UncommittedId);
				branch.CommitIds.Remove(Commit.UncommittedId);
				repository.Commits[Commit.UncommittedId].BranchId = localBranch.Id;
			}

			localBranch.FirstCommitId = localBranch.Commits.Last().Id;

			branch.IsMainPart = true;
			branch.LocalSubBranchId = localBranch.Id;
			localBranch.MainBranchId = branch.Id;
			if (branch.IsCurrent)
			{
				branch.IsCurrent = false;
			}

			branch.IsLocal = false;

			branch.TipCommitId = branch.RemoteTipCommitId;
		}


		private static MBranch CreateBranchBasedOnSubBranch(MSubBranch subBranch)
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