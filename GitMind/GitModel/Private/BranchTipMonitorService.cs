using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class BranchTipMonitorService : IBranchTipMonitorService
	{
		private static readonly string Origin = "origin/";

		private readonly IRepoCaller repoCaller;
		private readonly IGitCommitBranchNameService gitCommitBranchNameService;


		public BranchTipMonitorService(
			IRepoCaller repoCaller,
			IGitCommitBranchNameService gitCommitBranchNameService)
		{
			this.repoCaller = repoCaller;
			this.gitCommitBranchNameService = gitCommitBranchNameService;
		}


		public async Task CheckAsync(Repository repository)
		{
			await repoCaller.UseLibRepoAsync(repo =>
			{
				IReadOnlyDictionary<CommitSha, string> commitIds = GetSingleBranchTipCommits(repository, repo);

				foreach (var pair in commitIds)
				{
					BranchName branchName = pair.Value;
					if (branchName.StartsWith(Origin))
					{
						branchName = branchName.Substring(Origin.Length);
					}

					gitCommitBranchNameService.SetCommitBranchNameAsync(pair.Key, branchName);
				}

				gitCommitBranchNameService.PushNotesAsync(repository.RootCommit.RealCommitSha);
			});
		}


		private static IReadOnlyDictionary<CommitSha, string> GetSingleBranchTipCommits(
			Repository repository, 
			LibGit2Sharp.Repository repo)
		{
			Dictionary<CommitSha, LibGit2Sharp.Branch> branchByTip =
				new Dictionary<CommitSha, LibGit2Sharp.Branch>();

			foreach (LibGit2Sharp.Branch branch in repo.Branches)
			{
				if (branch.FriendlyName.EndsWith("/HEAD", StringComparison.Ordinal))
				{
					// Skip current (head) branch
					continue;
				}

				CommitSha commitSha = new CommitSha(branch.Tip.Sha);
				CommitId commitId = new CommitId(commitSha);

				// Check if commit has any children (i.e. is not sole branch tip)
				if (repository.Commits.TryGetValue(commitId, out Commit commit) && !commit.Children.Any())
				{
					if (!branchByTip.TryGetValue(commitSha, out LibGit2Sharp.Branch existingBranch))
					{
						// No existing branch has yet a tip to this commit
						branchByTip[commitSha] = branch;
					}
					else
					{
						// Some other branch points to this tip, lets check if it is remote/local pair
						if (existingBranch != null)
						{
							if (!AreLocalRemotePair(branch, existingBranch)
							    && !AreLocalRemotePair(existingBranch, branch))
							{
								// Multiple branches point to same commit, set to null to disable this commit id
								branchByTip[commitSha] = null;
								Log.Debug($"Multiple branches {commit}, {branch.FriendlyName} != {existingBranch.FriendlyName}");
							}
						}
						else
						{
							Log.Warn($"Multiple branch {commit}, {branch.FriendlyName}");
						}
					}
				}
			}

			return branchByTip
				.Where(pair => pair.Value != null)
				.Select(pair => new {pair.Key, pair.Value.FriendlyName})
				.ToDictionary(p => p.Key, p => p.FriendlyName);
		}


		private static bool AreLocalRemotePair(
			LibGit2Sharp.Branch branch1, LibGit2Sharp.Branch branch2)
		{
			return
				branch1.IsRemote &&
				branch2.IsTracking &&
				0 == Txt.CompareOic(branch2.TrackedBranch.CanonicalName, branch1.CanonicalName);
		}
	}
}