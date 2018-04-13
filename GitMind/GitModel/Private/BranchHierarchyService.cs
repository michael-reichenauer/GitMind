using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Common;
using GitMind.Features.Branches.Private;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	/// <summary>
	/// Handles branch names
	/// </summary>
	internal class BranchHierarchyService : IBranchHierarchyService
	{
		private readonly IGitBranchService gitBranchService;


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
				foreach (CommitId id in localPart.CommitIds)
				{
					branch.CommitIds.Add(id);
					repository.Commits[id].BranchId = branch.Id;
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
				}

				List<MCommit> commits = branch.Commits.ToList();

				// Using custom sort to ensure all commits are compared to each other.
				// This helps when commit dates are same and compare with parent is needed
				Sorter.Sort(commits, Compare.With<MCommit>(CompareCommitsDescending));

				branch.TipCommitId = commits.Any() ? commits.First().Id : branch.ParentCommitId;

				branch.FirstCommitId = commits.Any() ? commits.Last().Id : branch.ParentCommitId;
				branch.CommitIds = commits.Select(c => c.Id).ToList();

				if (!branch.CommitIds.Any())
				{
					if (branch.TipCommitId != CommitId.None)
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

		private static int CompareCommitsDescending(MCommit c1, MCommit c2)
		{
			if (c1 == c2)
			{
				return 0;
			}

			if (c1.CommitDate < c2.CommitDate)
			{
				return 1;
			}
			else if (c1.CommitDate > c2.CommitDate)
			{
				return -1;
			}
			else
			{
				if (c2.Parents.Any(c => c.Id == c1.Id))
				{
					return 1;
				}
				else if (c1.Parents.Any(c => c.Id == c2.Id))
				{
					return -1;
				}

				return 0;
			}
		}


		private static void GroupSubBranchesIntoOneMainBranch(
			IGrouping<CommitId, KeyValuePair<string, MSubBranch>> groupByBranch)
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

			branch.IsActive = activeSubBranches.Any();

			MSubBranch localSubBranch = activeSubBranches.FirstOrDefault(b => b.IsLocal);
			MSubBranch remoteSubBranch = activeSubBranches.FirstOrDefault(b => b.IsRemote);

			branch.IsLocal = localSubBranch != null;
			branch.LocalTipCommitId = localSubBranch?.TipCommitId ?? CommitId.None;
			branch.IsRemote = remoteSubBranch != null;
			branch.RemoteTipCommitId = remoteSubBranch?.TipCommitId ?? CommitId.None;
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

				string virtualShaText = (Guid.NewGuid() + Guid.NewGuid().ToString()).Replace("-", "")
					.Substring(0, 40);
				CommitSha virtualSha = new CommitSha(virtualShaText);
				CommitId virtualId = new CommitId(virtualShaText);

				MCommit commit = AddVirtualCommit(repository, virtualId);


				commit.IsVirtual = true;
				commit.BranchId = branch.Id;
				commit.SetBranchName(branch.Name);
				CopyToVirtualCommit(repository, branch, commit, virtualSha);
				SetChildOfParents(commit);

				//repository.Commits[commit.Id] = commit;

				branch.CommitIds.Add(commit.Id);
				branch.TipCommitId = commit.Id;
				branch.FirstCommitId = commit.Id;

				branch.TipCommit.BranchTips = $"{branch.Name} branch tip";
			}
		}

		private static MCommit AddVirtualCommit(
			MRepository repository, 
			CommitId virtualId)
		{
			MCommit commit = new MCommit()
			{
				Repository = repository,
				Id = virtualId,
			};

			repository.Commits[virtualId] = commit;

			return commit;
		}


		private void SetLocalAndRemoteAhead(MRepository repository)
		{
			IEnumerable<MBranch> unsynkedBranches = repository.Branches.Values
				.Where(b => 
					b.IsActive &&
					b.IsLocal && 
					b.IsRemote && 
					b.LocalTipCommitId != b.RemoteTipCommitId)
					.ToList();

			foreach (MBranch branch in unsynkedBranches)
			{
				MCommit localTipCommit = repository.Commits[branch.LocalTipCommitId];
				MCommit remoteTipCommit = repository.Commits[branch.RemoteTipCommitId];

				if (localTipCommit.Id == CommitId.Uncommitted)
				{
					localTipCommit = localTipCommit.FirstParent;
				}

				if (gitBranchService.GetCommonAncestor(
					localTipCommit.Sha, remoteTipCommit.Sha).HasValue(out var div))
				{
					CommitSha commonTip = div.CommonId;
					MCommit commonCommit = repository.Commits[new CommitId(commonTip.Sha)];

					commonCommit
						.CommitAndFirstAncestors()
						.Where(c => c.BranchId == branch.Id)
						.ForEach(c => c.IsCommon = true);

					branch.Commits.ForEach(commit =>
					{
						commit.IsLocalAhead = false;
						commit.IsRemoteAhead = false;
					});

					if (commonTip != localTipCommit.Sha || 
						(repository.Commits[branch.LocalTipCommitId].IsUncommitted 
							&& !repository.Commits[branch.FirstCommitId].IsUncommitted))
					{
						MakeLocalBranch(repository, branch, localTipCommit.Id, commonCommit.Id);
					}

					if (branch.IsLocal)
					{
						HashSet<CommitId> marked = new HashSet<CommitId>();
						int localCount = 0;
						Stack<MCommit> commits = new Stack<MCommit>();
						commits.Push(localTipCommit);

						while (commits.Any())
						{
							MCommit commit = commits.Pop();
							if (!marked.Contains(commit.Id) && !commit.IsCommon && commit.Branch == branch)
							{
								commit.IsLocalAhead = true;
								localCount++;
								marked.Add(commit.Id);
								commit.Parents
									.Where(p => p.Branch == branch)
									.ForEach(p => commits.Push(p));
							}
						}

						branch.LocalAheadCount = localCount;
					}

					if (branch.IsRemote)
					{
						HashSet<CommitId> marked = new HashSet<CommitId>();
						int remoteCount = 0;
						Stack<MCommit> commits = new Stack<MCommit>();
						commits.Push(remoteTipCommit);

						while (commits.Any())
						{
							MCommit commit = commits.Pop();

							if (!marked.Contains(commit.Id) && !commit.IsCommon && commit.Branch == branch)
							{
								commit.IsRemoteAhead = true;
								remoteCount++;
								marked.Add(commit.Id);
							
								commit.Parents
									.Where(p => p.Branch == branch)
									.ForEach(p => commits.Push(p));
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
			CommitId localTip,
			CommitId commonTip)
		{
			string name = $"{branch.Name}";
			string branchId = name + "(local)-" + commonTip;
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
			localBranch.IsActive = branch.IsActive;

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
					commit.Parents
						.Where(p => p.Branch == branch)
						.ForEach(p => commits.Push(p));
				}
			}

			localBranch.LocalAheadCount = localBranch.Commits.Count();
			localBranch.RemoteAheadCount = 0;

			if (repository.Commits.TryGetValue(CommitId.Uncommitted, out MCommit uncommitted)
				&& branch.TipCommitId == CommitId.Uncommitted)
			{
				localBranch.TipCommitId = uncommitted.Id;
				localBranch.LocalTipCommitId = uncommitted.Id;
				localBranch.CommitIds.Insert(0, uncommitted.Id);
				branch.CommitIds.Remove(uncommitted.Id);
				branch.TipCommitId = uncommitted.FirstParentId;
				uncommitted.BranchId = localBranch.Id;
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
			if (!branch.CommitIds.Any())
			{
				branch.FirstCommitId = branch.TipCommitId;
			}
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
				if (branch.Value.ParentCommitId != CommitId.None
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


		private static void CopyToVirtualCommit
			(MRepository repository, MBranch branch, MCommit commit, CommitSha virtualSha)
		{
			GitCommit gitCommit = new GitCommit(
				virtualSha,
				branch.ParentCommit.Subject,
				branch.ParentCommit.Message,
				branch.ParentCommit.Author,
				branch.ParentCommit.AuthorDate,
				branch.ParentCommit.CommitDate + TimeSpan.FromSeconds(1),
				new List<CommitId> { branch.ParentCommitId });

			repository.GitCommits[commit.Id] = gitCommit;

			// commit.Id = GetId();
		}


		private static void SetChildOfParents(MCommit commit)
		{
			bool isFirstParent = true;
			foreach (MCommit parent in commit.Parents)
			{
				IList<CommitId> childIds = parent.ChildIds;
				if (!childIds.Contains(commit.Id))
				{
					childIds.Add(commit.Id);
				}

				if (isFirstParent)
				{
					isFirstParent = false;
					IList<CommitId> firstChildIds = parent.FirstChildIds;
					if (!firstChildIds.Contains(commit.Id))
					{
						firstChildIds.Add(commit.Id);
					}
				}
			}
		}
	}
}