using System;
using System.Collections.Generic;

using System.Linq;
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


		public void AddActiveBranches(LibGit2Sharp.Repository repo, MRepository repository)
		{
			foreach (LibGit2Sharp.Branch gitBranch in repo.Branches)
			{
				string branchName = gitBranch.FriendlyName;
				if (branchName == "origin/HEAD" || branchName == "HEAD")
				{
					continue;
				}

				MSubBranch subBranch = ToBranch(gitBranch, repository);
				repository.SubBranches[subBranch.SubBranchId] = subBranch;
			}
		}


		public void AddInactiveBranches(MRepository repository)
		{
			// Commits which has no child, which has this commit as a first parent, i.e. it is the 
			// top of a branch and there is no existing branch at this commit
			List<string> activeBranches = repository.SubBranches
				.Where(b => b.Value.IsActive).Select(b => b.Value.LatestCommitId)
				.ToList();

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
					LatestCommitId = commit.Id,
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
							LatestCommitId = commit.Id,
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
							LatestCommitId = commit.Id,
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
			foreach (MCommit commit in subBranch.LatestCommit.FirstAncestors()
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


		private static MSubBranch ToBranch(LibGit2Sharp.Branch gitBranch, MRepository repository)
		{
			string branchName = gitBranch.FriendlyName;
			if (gitBranch.IsRemote && branchName.StartsWith(Origin))
			{
				branchName = branchName.Substring(Origin.Length);
			}

			return new MSubBranch
			{
				Repository = repository,
				SubBranchId = Guid.NewGuid().ToString(),
				Name = branchName,
				LatestCommitId = gitBranch.Tip.Sha,
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