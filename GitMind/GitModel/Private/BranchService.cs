using System;
using System.Collections.Generic;

using System.Linq;
using GitMind.Git;
using GitMind.Utils;


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


		public void AddActiveBranches(
			GitRepository gitRepository, GitStatus gitStatus, MRepository repository)
		{
			GitBranch currentBranch = gitRepository.Head;

			foreach (GitBranch gitBranch in gitRepository.Branches)
			{
				string branchName = gitBranch.Name;
				if (branchName == "origin/HEAD" || branchName == "HEAD")
				{
					continue;
				}

				MSubBranch subBranch = ToBranch(gitBranch, repository);
				repository.SubBranches[subBranch.SubBranchId] = subBranch;

				if (!gitStatus.OK && gitBranch.Name == currentBranch.Name && !gitBranch.IsRemote)
				{
					// Setting virtual uncommitted commit as tip of the current branch
					subBranch.TipCommitId = MCommit.UncommittedId;
					subBranch.TipCommit.SubBranchId = subBranch.SubBranchId;
				}
			}

			if (gitRepository.Head.Name == "(no branch)")
			{
				Log.Warn("No current branch (detached)");

				MSubBranch subBranch = ToBranch(gitRepository.Head, repository);
				repository.SubBranches[subBranch.SubBranchId] = subBranch;

				if (!gitStatus.OK && gitRepository.Head.Name == currentBranch.Name && !gitRepository.Head.IsRemote)
				{
					// Setting virtual uncommitted commit as tip of the current branch
					subBranch.TipCommitId = MCommit.UncommittedId;
					subBranch.TipCommit.SubBranchId = subBranch.SubBranchId;
				}
			}
		}


		public void AddInactiveBranches(MRepository repository)
		{
			// Get the list of active branch tips
			List<string> activeBranches = repository.SubBranches
				.Where(b => b.Value.IsActive).Select(b => b.Value.TipCommitId)
				.ToList();

			// Commits which has no child, which has this commit as a first parent, i.e. it is the 
			// top of a branch and there is no existing branch at this commit
			IEnumerable<MCommit> topCommits = repository.Commits.Values
				.Where(commit =>
					commit.BranchId == null
					&& commit.SubBranchId == null
					&& !commit.HasFirstChild
					&& !activeBranches.Contains(commit.Id));

			foreach (MCommit commit in topCommits)
			{
				MSubBranch subBranch = new MSubBranch
				{
					Repository = repository,
					SubBranchId = Guid.NewGuid().ToString(),
					TipCommitId = commit.Id,
				};

				string branchName = TryFindBranchName(commit);
				if (string.IsNullOrEmpty(branchName))
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

						string branchName = commit.BranchName;

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

						commit.BranchName = subBranch.Name;
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

						string branchName = AnonyousBranchPrefix + commit.ShortId;

						if (commit.FirstChildren.Count() > 1)
						{
							branchName = MultibranchPrefix + commit.ShortId;
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
							TipCommitId = commit.Id,
							IsActive = false,
						};

						subBranch.IsAnonymous = IsBranchNameAnonyous(branchName);
						subBranch.IsMultiBranch = IsBranchNameMultiBranch(branchName);

						repository.SubBranches[subBranch.SubBranchId] = subBranch;

						commit.BranchName = branchName;
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
				commit.BranchName = subBranch.Name;
				commit.SubBranchId = subBranch.SubBranchId;
			}
		}


		private static MSubBranch ToBranch(GitBranch gitBranch, MRepository repository)
		{
			string branchName = gitBranch.Name;
			if (gitBranch.IsRemote && branchName.StartsWith(Origin))
			{
				branchName = branchName.Substring(Origin.Length);
			}

			return new MSubBranch
			{
				Repository = repository,
				SubBranchId = Guid.NewGuid().ToString(),
				Name = branchName,
				TipCommitId = gitBranch.TipId,
				IsActive = true,
				IsRemote = gitBranch.IsRemote
			};
		}


		private string TryFindBranchName(MCommit root)
		{
			string branchName = commitBranchNameService.GetBranchName(root);

			if (string.IsNullOrEmpty(branchName))
			{
				// Could not find a branch name from the commit, lets try it ancestors
				foreach (MCommit commit in root.FirstAncestors()
					.TakeWhile(c => c.HasSingleFirstChild))
				{
					branchName = commitBranchNameService.GetBranchName(commit);
					if (!string.IsNullOrEmpty(branchName))
					{
						return branchName;
					}
				}
			}

			return branchName;
		}


		private bool IsBranchNameAnonyous(string branchName)
		{
			return
				branchName.StartsWith(AnonyousBranchPrefix)
				|| branchName.StartsWith(MultibranchPrefix);
		}


		private bool IsBranchNameMultiBranch(string branchName)
		{
			return branchName.StartsWith(MultibranchPrefix);
		}
	}
}