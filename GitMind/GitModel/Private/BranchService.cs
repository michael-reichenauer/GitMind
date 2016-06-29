using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Git;


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

			foreach (MCommit commit in topCommits)
			{
				MSubBranch subBranch = new MSubBranch
				{
					Repository = repository,
					SubBranchId = Guid.NewGuid().ToString(),
					LatestCommitId = commit.Id,
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


		public IReadOnlyList<MSubBranch> AddMissingInactiveBranches(
			IReadOnlyList<MCommit> commits, MRepository repository)
		{
			List<MSubBranch> branches = new List<MSubBranch>();

			bool isFound;
			do
			{
				isFound = false;
				foreach (MCommit commit in commits)
				{
					if (commit.HasBranchName && commit.SubBranchId == null)
					{
						isFound = true;

						MSubBranch subBranch = new MSubBranch
						{
							Repository = repository,
							Name = commit.BranchXName,
							SubBranchId = Guid.NewGuid().ToString(),
							LatestCommitId = commit.Id,
						};

						repository.SubBranches.Add(subBranch);
						branches.Add(subBranch);

						commit.SubBranchId = subBranch.SubBranchId;

						SetSubBranchCommits(subBranch);
					}
				}
			} while (isFound);

			return branches;
		}


		public IReadOnlyList<MSubBranch> AddMultiBranches(
			IReadOnlyList<MCommit> commits, MRepository repository)
		{
			List<MSubBranch> multiBranches = new List<MSubBranch>();

			bool isFound;
			do
			{
				isFound = false;
				foreach (MCommit commit in commits)
				{
					if (!commit.HasBranchName)
					{
						isFound = true;

						string branchName = "Branch_" + commit.ShortId;
						bool isMultiBranch = false;

						if (commit.FirstChildren.Count() > 1)
						{
							MCommit firstChild = commit.FirstChildren.ElementAt(0);

							if (!commit.FirstChildren.All(
								c => c.HasBranchName && c.BranchXName == firstChild.BranchXName))
							{
								// Not all children have the same name (or none have a name)

								branchName = "Multibranch_" + commit.ShortId;
								isMultiBranch = true;
							}
						}
						else
						{
							string commitBranchName = commitBranchNameService.GetBranchName(commit);
							if (commitBranchName != null)
							{
								branchName = commitBranchName;
							}
						}

						MSubBranch subBranch = new MSubBranch
						{
							Repository = repository,
							SubBranchId = Guid.NewGuid().ToString(),
							Name = branchName,
							LatestCommitId = commit.Id,
							IsMultiBranch = isMultiBranch,
							IsActive = false,
							IsAnonymous = true
						};

						repository.SubBranches.Add(subBranch);
						multiBranches.Add(subBranch);

						commit.BranchXName = branchName;
						commit.SubBranchId = subBranch.SubBranchId;

						SetSubBranchCommits(subBranch);
					}
				}

			} while (isFound);

			return multiBranches;
		}


		private void SetSubBranchCommits(MSubBranch subBranch)
		{
			foreach (MCommit commit in subBranch.LatestCommit.FirstAncestors()
				.TakeWhile(c =>
					c.SubBranchId == null
					&& (commitBranchNameService.GetBranchName(c) == null
					    || commitBranchNameService.GetBranchName(c) == subBranch.Name)
					&& !c.FirstChildren.Any(fc => fc.BranchXName != subBranch.Name)))
			{
				commit.BranchXName = subBranch.Name;
				commit.SubBranchId = subBranch.SubBranchId;
			}
		}


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
	}
}