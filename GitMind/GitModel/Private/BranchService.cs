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


		public void AddActiveBranches(IReadOnlyList<GitBranch> gitBranches, MRepository repository)
		{
			foreach (GitBranch gitBranch in gitBranches)
			{
				MSubBranch subBranch = ToBranch(gitBranch, repository);
				repository.SubBranches[subBranch.SubBranchId] = subBranch;
			}
		}


		public void AddInactiveBranches(MRepository repository)
		{
			// Commits which has no child, which has this commit as a first parent, i.e. it is the 
			// top of a branch and there is no existing branch at this commit
			IEnumerable<MCommit> topCommits = repository.Commits
				.Where(commit =>
					!commit.Value.FirstChildren.Any()
					&& !repository.SubBranches.Any(b => b.Value.LatestCommitId == commit.Value.Id))
				.Select(c => c.Value);

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

				repository.SubBranches[subBranch.SubBranchId] = subBranch;
			}
		}


		public void AddMissingInactiveBranches(MRepository repository)
		{
			bool isFound;
			do
			{
				isFound = false;
				foreach (var commit in repository.Commits)
				{
					if (commit.Value.HasBranchName && commit.Value.SubBranchId == null)
					{
						isFound = true;

						MSubBranch subBranch = new MSubBranch
						{
							Repository = repository,
							Name = commit.Value.BranchName,
							SubBranchId = Guid.NewGuid().ToString(),
							LatestCommitId = commit.Value.Id,
						};

						repository.SubBranches[subBranch.SubBranchId] = subBranch;
						commit.Value.SubBranchId = subBranch.SubBranchId;

						SetSubBranchCommits(subBranch);
					}
				}
			} while (isFound);
		}


		public void AddMultiBranches(MRepository repository)
		{
			bool isFound;
			do
			{
				isFound = false;
				foreach (var commit in repository.Commits)
				{
					if (!commit.Value.HasBranchName)
					{
						isFound = true;

						string branchName = "Branch_" + commit.Value.ShortId;
						bool isMultiBranch = false;

						if (commit.Value.FirstChildren.Count() > 1)
						{
							branchName = "Multibranch_" + commit.Value.ShortId;
							isMultiBranch = true;
						}
						else
						{
							string commitBranchName = commitBranchNameService.GetBranchName(commit.Value);
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
							LatestCommitId = commit.Value.Id,
							IsMultiBranch = isMultiBranch,
							IsActive = false,
							IsAnonymous = true,
						};

						repository.SubBranches[subBranch.SubBranchId] = subBranch;

						commit.Value.BranchName = branchName;
						commit.Value.SubBranchId = subBranch.SubBranchId;

						SetSubBranchCommits(subBranch);
					}
				}

			} while (isFound);
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