using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Common;
using GitMind.Features.StatusHandling;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal class BranchService : IBranchService
	{
		private static readonly string Origin = "origin/";
		private static readonly string AnonyousBranchPrefix = "_Branch_";
		private readonly string MultibranchPrefix = "_Multibranch_";

		private readonly ICommitBranchNameService commitBranchNameService;


		public BranchService()
			: this(new CommitBranchNameService())
		{
		}


		public BranchService(ICommitBranchNameService commitBranchNameService)
		{
			this.commitBranchNameService = commitBranchNameService;
		}


		public void AddActiveBranches(GitRepository gitRepository, MRepository repository)
		{
			Status status = repository.Status;

			GitBranch currentBranch = gitRepository.Head;

			foreach (GitBranch gitBranch in gitRepository.Branches)
			{
				BranchName branchName = gitBranch.Name;
				if (branchName == BranchName.OriginHead || branchName == BranchName.Head)
				{
					continue;
				}

				MSubBranch subBranch = ToBranch(gitBranch, repository);
				repository.SubBranches[subBranch.SubBranchId] = subBranch;

				if (!status.IsOK && gitBranch.IsCurrent && !gitBranch.IsRemote)
				{
					// Setting virtual uncommitted commit as tip of the current branch
					subBranch.TipCommitId = repository.Uncommitted.Id;
					subBranch.TipCommit.SubBranchId = subBranch.SubBranchId;
				}
			}

			if (currentBranch.IsDetached)
			{
				MSubBranch subBranch = ToBranch(currentBranch, repository);
				repository.SubBranches[subBranch.SubBranchId] = subBranch;

				if (!status.IsOK)
				{
					// Setting virtual uncommitted commit as tip of the detached branch
					subBranch.TipCommitId = repository.Uncommitted.Id;
					subBranch.TipCommit.SubBranchId = subBranch.SubBranchId;
				}
			}
		}


		public void AddInactiveBranches(MRepository repository)
		{
			// Get the list of active branch tips
			List<CommitId> activeBranches = repository.SubBranches
				.Where(b => b.Value.IsActive)
				.Select(b => b.Value.TipCommitId)
				.ToList();

			// Commits which has no child, which has this commit as a first parent, i.e. it is the 
			// top of a branch and there is no existing branch at this commit
			IEnumerable<MCommit> topCommits = repository.Commits.Values
				.Where(commit =>
					commit.BranchId == null
					&& commit.SubBranchId == null
					&& !commit.HasFirstChild
					&& !activeBranches.Contains(commit.Id))
				.ToList();

			foreach (MCommit commit in topCommits)
			{
				MSubBranch subBranch = new MSubBranch
				{
					Repository = repository,
					SubBranchId = Guid.NewGuid().ToString(),
					TipCommitId = commit.Id,
				};

				BranchName branchName = TryFindBranchName(commit);
				if (branchName == null)
				{
					branchName = AnonyousBranchPrefix + commit.ShortId;
				}

				subBranch.IsAnonymous = IsBranchNameAnonyous(branchName);
				subBranch.IsMultiBranch = IsBranchNameMultiBranch(branchName);
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
				foreach (var commit in repository.Commits.Values)
				{
					if (commit.BranchId == null && commit.HasBranchName && commit.SubBranchId == null)
					{
						isFound = true;

						BranchName branchName = commit.BranchName;

						MSubBranch subBranch = new MSubBranch
						{
							Repository = repository,
							Name = branchName,
							SubBranchId = Guid.NewGuid().ToString(),
							TipCommitId = commit.Id,
						};

						subBranch.IsAnonymous = IsBranchNameAnonyous(branchName);
						subBranch.IsMultiBranch = IsBranchNameMultiBranch(branchName);

						repository.SubBranches[subBranch.SubBranchId] = subBranch;

						commit.SetBranchName(subBranch.Name);
						commit.SubBranchId = subBranch.SubBranchId;

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
				foreach (var commit in repository.Commits.Values)
				{
					if (commit.BranchId == null && !commit.HasBranchName)
					{
						isFound = true;

						BranchName branchName = AnonyousBranchPrefix + commit.ShortId;

						if (commit.FirstChildren.Count() > 1)
						{
							branchName = MultibranchPrefix + commit.ShortId;
						}
						else
						{
							BranchName commitBranchName = commitBranchNameService.GetBranchName(commit);
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
							TipCommitId = commit.Id,
							IsActive = false,
						};

						subBranch.IsAnonymous = IsBranchNameAnonyous(branchName);
						subBranch.IsMultiBranch = IsBranchNameMultiBranch(branchName);

						repository.SubBranches[subBranch.SubBranchId] = subBranch;

						commit.SetBranchName(branchName);
						commit.SubBranchId = subBranch.SubBranchId;
						SetSubBranchCommits(subBranch);
					}
				}

			} while (isFound);
		}


		private void SetSubBranchCommits(MSubBranch subBranch)
		{
			foreach (MCommit commit in subBranch.TipCommit.FirstAncestors()
				.TakeWhile(c =>
					c.BranchId == null
					&& c.SubBranchId == null
					&& (commitBranchNameService.GetBranchName(c) == null
							|| commitBranchNameService.GetBranchName(c) == subBranch.Name)
					&& !c.FirstChildren.Any(fc => fc.BranchName != subBranch.Name)))
			{
				commit.SetBranchName(subBranch.Name);
				commit.SubBranchId = subBranch.SubBranchId;
			}
		}


		private static MSubBranch ToBranch(GitBranch gitBranch, MRepository repository)
		{
			BranchName branchName = gitBranch.Name;
			if (gitBranch.IsRemote && branchName.StartsWith(Origin))
			{
				branchName = branchName.Substring(Origin.Length);
			}

			return new MSubBranch
			{
				Repository = repository,
				SubBranchId = Guid.NewGuid().ToString(),
				Name = branchName,
				TipCommitId = repository.Commit(new CommitId(gitBranch.TipId)).Id,
				IsActive = true,
				IsCurrent = gitBranch.IsCurrent,
				IsDetached = gitBranch.IsDetached,
				IsRemote = gitBranch.IsRemote
			};
		}


		private BranchName TryFindBranchName(MCommit root)
		{
			BranchName branchName = commitBranchNameService.GetBranchName(root);

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


		private bool IsBranchNameAnonyous(BranchName branchName)
		{
			return
				branchName.StartsWith(AnonyousBranchPrefix)
				|| branchName.StartsWith(MultibranchPrefix);
		}


		private bool IsBranchNameMultiBranch(BranchName branchName)
		{
			return branchName.StartsWith(MultibranchPrefix);
		}
	}
}