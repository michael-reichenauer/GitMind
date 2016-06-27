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


		public IReadOnlyList<MSubBranch> AddActiveBranches(
			IReadOnlyList<GitBranch> gitBranches, MRepository repository)
		{
			return gitBranches.Select(gitBranch =>
			{
				MSubBranch subBranch = ToBranch(gitBranch, repository);
				repository.SubBranches.Add(subBranch);
				return subBranch;
			})
			.ToList();
		}

		public IReadOnlyList<MSubBranch> AddInactiveBranches(
			IReadOnlyList<MCommit> commits, MRepository repository)
		{
			List<MSubBranch> branches = new List<MSubBranch>();

			// Commits which has no child, which has this commit as a first parent, i.e. it is the 
			// top of a branch and there is no existing branch at this commit
			IEnumerable<MCommit> topCommits = commits.Where(commit =>
				!commit.FirstChildIds.Any()
				&& !repository.SubBranches.Any(b => b.LatestCommitId == commit.Id));

			//IEnumerable<MCommit> pullMergeTopCommits = commits
			//	.Where(commit =>
			//		commit.HasSecondParent
			//		&& commit.MergeSourceBranchNameFromSubject != null
			//		&& commit.MergeSourceBranchNameFromSubject == commit.MergeTargetBranchNameFromSubject)
			//	.Select(c => c.SecondParent);

			//topCommits = topCommits.Concat(pullMergeTopCommits).Distinct();

			foreach (MCommit commit in topCommits)
			{
				MSubBranch subBranch = new MSubBranch
				{
					Repository = repository,
					SubBranchId = Guid.NewGuid().ToString(),
					LatestCommitId = commit.Id,
					IsMultiBranch = false,
					IsActive = false
				};

				string branchName = TryFindBranchName(commit);
				if (string.IsNullOrEmpty(branchName))
				{
					branchName = "Branch_" + commit.ShortId;
					subBranch.IsAnonymous = true;
				}

				subBranch.Name = branchName;

				repository.SubBranches.Add(subBranch);
				branches.Add(subBranch);
			}

			return branches;
		}


		public IReadOnlyList<MSubBranch> AddMultiBranches(
			IReadOnlyList<MCommit> commits, 
			//IReadOnlyList<MSubBranch> branches, 
			MRepository repository)
		{
			// Commits, which do not have branch names
			IEnumerable<MCommit> roots = commits.Where(c => !c.HasBranchName);

			//// The commits where multiple branches are starting and the commits has no branch name
			//IEnumerable<MCommit> roots2 = branches
			//	.GroupBy(b => b.LatestCommitId)
			//	.Where(group => group.Count() > 1)
			//	.Select(group => repository.Commits[group.Key])
			//	.Where(c => string.IsNullOrEmpty(c.BranchXName));

		
			//roots = roots.Concat(roots2);

			List<MSubBranch> multiBranches = new List<MSubBranch>();
			foreach (MCommit root in roots)
			{
				string branchName = "Multibranch_" + root.ShortId;
				bool isMultiBranch = true;

				if (root.Children.Any() &&
				    root.Children.All(c => c.HasBranchName && c.BranchXName == root.Children.ElementAt(0).BranchXName))
				{
					// All children have the same branch name thus this branch is just a continuation of them
					MCommit commit = root.Children.ElementAt(0);
					branchName = commit.BranchXName;
					isMultiBranch = branchName.StartsWith("Multibranch_");
					//Log.Warn($"Multi branch with all same parents {branchName}");
				}

				MSubBranch subBranch = new MSubBranch
				{
					Repository = repository,
					SubBranchId = Guid.NewGuid().ToString(),
					Name = branchName,
					LatestCommitId = root.Id,
					IsMultiBranch = isMultiBranch,
					IsActive = false,
					IsAnonymous = true
				};

				repository.SubBranches.Add(subBranch);
				multiBranches.Add(subBranch);

				root.BranchXName = branchName;
				root.SubBranchId = subBranch.SubBranchId;
			}

			return multiBranches;
		}


		public IReadOnlyList<MSubBranch> AddMissingInactiveBranches(
			IReadOnlyList<MCommit> commits, MRepository repository)
		{
			List<MSubBranch> branches = new List<MSubBranch>();

			// Commits, which do have branch names but no sub branch id
			IEnumerable<MCommit> topCommits = commits
				.Where(c => c.HasBranchName && c.SubBranchId == null).ToList();

			foreach (MCommit commit in topCommits)
			{
				MSubBranch subBranch = new MSubBranch
				{
					Repository = repository,
					SubBranchId = Guid.NewGuid().ToString(),
					LatestCommitId = commit.Id,
					IsMultiBranch = false,
					IsActive = false
				};
	
				subBranch.Name = commit.BranchXName;
				commit.SubBranchId = subBranch.SubBranchId;

				repository.SubBranches.Add(subBranch);
				branches.Add(subBranch);
			}

			return branches;
		}


		public void SetBranchHierarchy(
			IReadOnlyList<MSubBranch> subBranches, MRepository repository)
		{
			SetParentCommitId(subBranches);

			GroupSubBranches(subBranches);
			SetBranchHierarchy(repository.Branches);
		}


		private void SetSubBranchId(IReadOnlyList<MSubBranch> subBranches)
		{
			foreach (MSubBranch branch in subBranches)
			{
				//SetSubBranchId(branch.LatestCommit, branch);

				foreach (var commit in branch.LatestCommit.FirstAncestors()
					.Where(c => c.SubBranchId == null && c.BranchXName == branch.Name))
				{
					commit.SubBranchId = branch.SubBranchId;
				}
			}
		}


		//private void SetSubBranchId(MCommit commit, MSubBranch branch)
		//{
		//	if (commit.SubBranchId != null || commit.BranchXName != branch.Name

		//	{
		//		return;
		//	}
		//	commit.SubBranchId = branch.Id;
		//	foreach (MCommit parent in commit.Parents)
		//	{
		//		SetSubBranchId(parent, branch);
		//	}
		//}


		private static MSubBranch ToBranch(GitBranch gitBranch, MRepository mRepository)
		{
			return new MSubBranch
			{
				Repository = mRepository,
				SubBranchId = Guid.NewGuid().ToString(),
				Name = gitBranch.Name,
				LatestCommitId = gitBranch.LatestCommitId,
				IsMultiBranch = false,
				IsActive = true,
				IsRemote = gitBranch.IsRemote
			};
		}

	


		private string TryFindBranchName(MCommit root)
		{
			string branchName = commitBranchNameService.GetBranchName(root);

			if (branchName == null)
			{
				// Could not find a branch name from the commit, lets try it ancestors
				foreach (MCommit commit in root.FirstAncestors()
					.TakeWhile(c => c.HasSingleFirstChild))
				{
					branchName = commitBranchNameService.GetBranchName(commit);
					if (branchName != null)
					{
						return branchName;
					}
				}
			}

			return branchName;
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

					mBranch.SubBrancheIds.AddRange(groupByBranch.Select(b => b.SubBranchId));
					mBranch.SubBranches.ForEach(b => b.BranchId = mBranch.Id);

					mBranch.CommitIds.AddRange(
						groupByBranch
							.SelectMany(branch =>
								new[] { branch.LatestCommit }
									.Where(c => c.SubBranchId == branch.SubBranchId && c.Id != branch.ParentCommitId)
									.Concat(
										branch.LatestCommit
											.FirstAncestors()
											.TakeWhile(c => c.SubBranchId == branch.SubBranchId && c.Id != branch.ParentCommitId)))
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


		private static void SetBranchHierarchy(IReadOnlyList<MBranch> branches)
		{
			foreach (MBranch branch in branches)
			{
				if (branch.ParentCommitId != null && branch.ParentCommit.BranchId != branch.Id)
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

			//foreach (XBranch xBranch in branches.Where(b => b.ParentBranchId == null))
			//{
			//	LogBranchHierarchy(xBranch, 0);
			//}
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