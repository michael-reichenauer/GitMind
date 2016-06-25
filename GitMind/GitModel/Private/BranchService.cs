using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class BranchService : IBranchService
	{
		private readonly ICommitBranchNameService commitBranchNameService;


		public BranchService()
			: this(new CommitBranchNameService())
		{			
		}

		public BranchService(ICommitBranchNameService commitBranchNameService)
		{
			this.commitBranchNameService = commitBranchNameService;
		}


		public IReadOnlyList<MSubBranch> AddSubBranches(
			IReadOnlyList<GitBranch> gitBranches,
			MRepository mRepository,
			IReadOnlyList<MCommit> commits)
		{
			Timing t = new Timing();
			IReadOnlyList<MSubBranch> activeBranches = AddActiveBranches(gitBranches, mRepository);
			t.Log("Added branches");
			Log.Debug($"Active sub branches {activeBranches.Count} ({mRepository.SubBranches.Count})");

			IReadOnlyList<MSubBranch> inactiveBranches = AddInactiveBranches(commits, mRepository);
			IReadOnlyList<MSubBranch> branches = activeBranches.Concat(inactiveBranches).ToList();
			t.Log("Inactive subbranches");
			Log.Debug($"Inactive sub branches {inactiveBranches.Count} ({mRepository.SubBranches.Count})");
			//branches2.ForEach(b => Log.Debug($"   Branch {b}"));
			return branches;
		}


		public IReadOnlyList<MSubBranch> AddMultiBranches(
			IReadOnlyList<MCommit> commits, 
			IReadOnlyList<MSubBranch> branches, 
			MRepository xmodel)
		{
			IEnumerable<MCommit> roots =
				commits.Where(c =>
					string.IsNullOrEmpty(c.BranchXName)
					&& c.FirstChildIds.Count > 1);

			// The commits where multiple branches are starting and the commits has no branch name
			IEnumerable<MCommit> roots2 = branches
				.GroupBy(b => b.LatestCommitId)
				.Where(group => @group.Count() > 1)
				.Select(group => xmodel.Commits[@group.Key])
				.Where(c => string.IsNullOrEmpty(c.BranchXName));

			roots = roots.Concat(roots2);

			List<MSubBranch> multiBranches = new List<MSubBranch>();
			foreach (MCommit root in roots)
			{
				string branchName = "Multibranch_" + root.ShortId;

				if (root.Children.Any() &&
				    root.Children.All(c => c.HasBranchName && c.BranchXName == root.Children.ElementAt(0).BranchXName))
				{
					// All children have the same branch name thus this branch is just a continuation of them
					branchName = root.Children.ElementAt(0).BranchXName;
				}

				MSubBranch subBranch = new MSubBranch
				{
					Repository = xmodel,
					Id = Guid.NewGuid().ToString(),
					Name = branchName,
					LatestCommitId = root.Id,
					IsMultiBranch = true,
					IsActive = false,
					IsAnonymous = true
				};

				xmodel.SubBranches.Add(subBranch);
				multiBranches.Add(subBranch);
			}

			return multiBranches;
		}


		public void SetBranchHierarchy(
			IReadOnlyList<MSubBranch> subBranches, MRepository mRepository)
		{
			SetParentCommitId(subBranches);
			GroupSubBranches(subBranches);
			SetBranchHierarchy(mRepository.Branches);
		}


	


		private IReadOnlyList<MSubBranch> AddActiveBranches(
			IReadOnlyList<GitBranch> gitBranches, MRepository xmodel)
		{
			return gitBranches.Select(gitBranch =>
			{
				MSubBranch subBranch = ToBranch(gitBranch, xmodel);
				xmodel.SubBranches.Add(subBranch);
				return subBranch;
			})
				.ToList();
		}



		private MSubBranch ToBranch(GitBranch gitBranch, MRepository mRepository)
		{
			string latestCommitId = gitBranch.LatestCommitId;

			return new MSubBranch
			{
				Repository = mRepository,
				Id = Guid.NewGuid().ToString(),
				Name = gitBranch.Name,
				LatestCommitId = latestCommitId,
				IsMultiBranch = false,
				IsActive = true,
				IsRemote = gitBranch.IsRemote
			};
		}

		private IReadOnlyList<MSubBranch> AddInactiveBranches(
		IReadOnlyList<MCommit> commits, MRepository mRepository)
		{
			List<MSubBranch> branches = new List<MSubBranch>();

			// Commits which has no child, which has this commit as a first parent, i.e. it is the 
			// top of a branch and there is no existing branch at this commit
			IEnumerable<MCommit> topCommits = commits.Where(commit =>
				!commit.FirstChildIds.Any()
				&& !mRepository.SubBranches.Any(b => b.LatestCommitId == commit.Id));

			IEnumerable<MCommit> pullMergeTopCommits = commits
				.Where(commit =>
					commit.HasSecondParent
					&& commit.MergeSourceBranchNameFromSubject != null
					&& commit.MergeSourceBranchNameFromSubject == commit.MergeTargetBranchNameFromSubject)
				.Select(c => c.SecondParent);

			topCommits = topCommits.Concat(pullMergeTopCommits).Distinct();


			foreach (MCommit xCommit in topCommits)
			{
				MSubBranch subBranch = new MSubBranch
				{
					Repository = mRepository,
					Id = Guid.NewGuid().ToString(),
					LatestCommitId = xCommit.Id,
					IsMultiBranch = false,
					IsActive = false
				};

				string branchName = TryFindBranchName(xCommit);
				if (string.IsNullOrEmpty(branchName))
				{
					branchName = "Branch_" + xCommit.ShortId;
					subBranch.IsAnonymous = true;
				}

				subBranch.Name = branchName;

				mRepository.SubBranches.Add(subBranch);
				branches.Add(subBranch);
			}


			return branches;
		}


		private string TryFindBranchName(MCommit mCommit)
		{
			string branchName = commitBranchNameService.GetBranchName(mCommit);

			if (branchName == null)
			{
				// Could not find a branch name from the commit, lets try it ancestors
				foreach (MCommit commit in mCommit.FirstAncestors()
					.TakeWhile(c => c.HasSingleFirstChild))
				{
					string name = commitBranchNameService.GetBranchName(commit);
					if (name != null)
					{
						return name;
					}
				}
			}

			return branchName;
		}


		private static void SetBranchHierarchy(IReadOnlyList<MBranch> branches)
		{
			foreach (MBranch xBranch in branches)
			{
				if (xBranch.ParentCommitId != null && xBranch.ParentCommit.BranchId != xBranch.Id)
				{
					xBranch.ParentBranchId = xBranch.ParentCommit.BranchId;

					MBranch parentBranch = xBranch.ParentBranch;
					if (!parentBranch.ChildBranches.Contains(xBranch))
					{
						parentBranch.ChildBrancheIds.Add(xBranch.Id);
					}
				}
				else
				{
					Log.Debug($"Branch {xBranch} has no parent branch");
				}
			}

			//foreach (XBranch xBranch in branches.Where(b => b.ParentBranchId == null))
			//{
			//	LogBranchHierarchy(xBranch, 0);
			//}
		}



		private static void SetParentCommitId(IReadOnlyList<MSubBranch> subBranches)
		{
			foreach (MSubBranch subBranch in subBranches)
			{
				MCommit LatestCommit = subBranch.LatestCommit;

				IEnumerable<MCommit> commits = subBranch.LatestCommit.FirstAncestors()
					.TakeWhile(c => c.BranchXName == subBranch.Name);

				if (commits.Any())
				{
					MCommit firstCommit = commits.Last();
					subBranch.FirstCommitId = firstCommit.Id;
					subBranch.ParentCommitId = firstCommit.FirstParentId;
				}
				else
				{
					if (LatestCommit.BranchXName != null)
					{
						subBranch.FirstCommitId = LatestCommit.Id;
						subBranch.ParentCommitId = LatestCommit.FirstParentId;
					}
					else
					{
						Log.Warn($"Branch with no commits {subBranch}");
					}
				}
			}
		}


		private static void GroupSubBranches(IReadOnlyList<MSubBranch> branches)
		{
			var groupedOnName = branches.GroupBy(b => b.Name);

			foreach (var groupByName in groupedOnName)
			{
				var groupedByParentCommitId = groupByName.GroupBy(b => b.ParentCommitId);

				foreach (var groupByBranch in groupedByParentCommitId)
				{
					MSubBranch subBranch = groupByBranch.First();
					MBranch mBranch = new MBranch
					{
						Repository = subBranch.Repository,
						Name = subBranch.Name,
						IsMultiBranch = subBranch.IsMultiBranch,
						IsActive = subBranch.IsActive,
						IsAnonymous = subBranch.IsAnonymous,
						ParentCommitId = subBranch.ParentCommitId
					};

					mBranch.Id = subBranch.Name + "-" + subBranch.ParentCommitId;

					mBranch.SubBrancheIds.AddRange(groupByBranch.Select(b => b.Id));
					mBranch.SubBranches.ForEach(b => b.BranchId = mBranch.Id);

					mBranch.CommitIds.AddRange(
						groupByBranch
							.SelectMany(branch =>
								new[] { branch.LatestCommit }
									.Where(c => c.SubBranchId == branch.Id && c.Id != branch.ParentCommitId)
									.Concat(
										branch.LatestCommit
											.FirstAncestors()
											.TakeWhile(c => c.SubBranchId == branch.Id && c.Id != branch.ParentCommitId)))
							.Distinct()
							.OrderByDescending(c => c.CommitDate)
							.Select(c => c.Id));

					if (mBranch.Commits.Any(c => c.BranchId != null))
					{
						Log.Error($"Commits belong to multiple branches {mBranch}");
					}

					mBranch.Commits.ForEach(c => c.BranchId = mBranch.Id);

					mBranch.LatestCommitId = mBranch.Commits.Any()
						? mBranch.Commits.First().Id
						: mBranch.ParentCommitId;
					mBranch.FirstCommitId = mBranch.Commits.Any()
						? mBranch.Commits.Last().Id
						: mBranch.ParentCommitId;

					mBranch.Repository.Branches.Add(mBranch);
				}
			}
		}


		private void LogBranchHierarchy(MBranch mBranch, int indent)
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