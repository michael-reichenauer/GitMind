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


		public IReadOnlyList<MSubBranch> AddInactiveBranches(MRepository repository)
		{
			List<MSubBranch> branches = new List<MSubBranch>();

			// Commits which has no child, which has this commit as a first parent, i.e. it is the 
			// top of a branch and there is no existing branch at this commit
			IEnumerable<MCommit> topCommits = repository.CommitList.Where(commit =>
				!commit.FirstChildren.Any()
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


		public IReadOnlyList<MSubBranch> AddMissingInactiveBranches(MRepository repository)
		{
			List<MSubBranch> branches = new List<MSubBranch>();

			bool isFound;
			do
			{
				isFound = false;
				foreach (MCommit commit in repository.CommitList)
				{
					if (commit.HasBranchName && commit.SubBranchId == null)
					{
						isFound = true;

						MSubBranch subBranch = new MSubBranch
						{
							Repository = repository,
							Name = commit.BranchName,
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


		public IReadOnlyList<MSubBranch> AddMultiBranches(MRepository repository)
		{
			List<MSubBranch> multiBranches = new List<MSubBranch>();

			bool isFound;
			do
			{
				isFound = false;
				foreach (MCommit commit in repository.CommitList)
				{
					if (!commit.HasBranchName)
					{
						isFound = true;

						string branchName = "Branch_" + commit.ShortId;
						bool isMultiBranch = false;

						if (commit.FirstChildren.Count() > 1)
						{
							branchName = "Multibranch_" + commit.ShortId;
							isMultiBranch = true;
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
							IsAnonymous = true,
						};

						repository.SubBranches.Add(subBranch);
						multiBranches.Add(subBranch);

						commit.BranchName = branchName;
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
					&& !c.FirstChildren.Any(fc => fc.BranchName != subBranch.Name)))
			{
				commit.BranchName = subBranch.Name;
				commit.SubBranchId = subBranch.SubBranchId;
			}
		}


		private static MSubBranch ToBranch(GitBranch gitBranch, MRepository repository)
		{
			return new MSubBranch
			{
				Repository = repository,
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