using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Common;
using GitMind.Features.Commits.Private;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;


namespace GitMind.GitModel.Private
{
	internal class BranchTipMonitorService : IBranchTipMonitorService
	{
		private static readonly string Origin = "origin/";

		private readonly IGitBranchService gitBranchService;
		private readonly IGitCommitBranchNameService gitCommitBranchNameService;


		public BranchTipMonitorService(
			IGitBranchService gitBranchService,
			IGitCommitBranchNameService gitCommitBranchNameService)
		{
			this.gitBranchService = gitBranchService;
			this.gitCommitBranchNameService = gitCommitBranchNameService;
		}


		public async Task CheckAsync(Repository repository)
		{
			Log.Debug("Checking branch tips ...");
			var branches = await gitBranchService.GetBranchesAsync(CancellationToken.None);
			if (branches.IsFaulted)
			{
				return;
			}

			IReadOnlyDictionary<CommitSha, string> commitIds = GetSingleBranchTipCommits(repository, branches.Value);

			foreach (var pair in commitIds)
			{
				BranchName branchName = pair.Value;
				if (branchName.StartsWith(Origin))
				{
					branchName = branchName.Substring(Origin.Length);
				}

				await gitCommitBranchNameService.SetCommitBranchNameAsync(pair.Key, branchName);
			}

			if (Settings.Get<Options>().DisableAutoUpdate)
			{
				Log.Info("DisableAutoUpdate = true");
				return;
			}

			await gitCommitBranchNameService.PushNotesAsync(repository.RootCommit.RealCommitSha);
		}


		private static IReadOnlyDictionary<CommitSha, string> GetSingleBranchTipCommits(
			Repository repository,
			IReadOnlyList<GitBranch> branches)
		{
			Dictionary<CommitSha, GitBranch> branchByTip = new Dictionary<CommitSha, GitBranch>();

			foreach (GitBranch branch in branches)
			{
				try
				{
					CommitSha commitSha = branch.TipSha;
					CommitId commitId = new CommitId(commitSha);

					// Check if commit has any children (i.e. is not sole branch tip)
					if (repository.Commits.TryGetValue(commitId, out Commit commit) && !commit.Children.Any())
					{
						if (!branchByTip.TryGetValue(commitSha, out GitBranch existingBranch))
						{
							// No existing branch has yet a tip to this commit
							branchByTip[commitSha] = branch;
						}
						else
						{
							try
							{
								// Some other branch points to this tip, lets check if it is remote/local pair
								if (existingBranch != null)
								{
									try
									{
										if (!AreLocalRemotePair(branch, existingBranch)
																			&& !AreLocalRemotePair(existingBranch, branch))
										{
											// Multiple branches point to same commit, set to null to disable this commit id
											branchByTip[commitSha] = null;
											Log.Debug($"Multiple branches {commit}, {branch.Name} != {existingBranch.Name}");
										}
									}
									catch (Exception e)
									{
										Log.Exception(e, $"Failed for {branch}");
										throw;
									}
								}
								else
								{
									Log.Warn($"Multiple branch {commit}, {branch.Name}");
								}
							}
							catch (Exception e)
							{
								Log.Exception(e, $"Failed for {branch}");
								throw;
							}
						}
					}
				}
				catch (Exception e)
				{
					Log.Exception(e, $"Failed for {branch}");
					throw;
				}
			}

			return branchByTip
				.Where(pair => pair.Value != null)
				.Select(pair => new { pair.Key, pair.Value.Name })
				.ToDictionary(p => p.Key, p => p.Name);
		}


		private static bool AreLocalRemotePair(GitBranch branch1, GitBranch branch2)
		{
			return
				branch1.IsRemote &&
				branch2.IsTracking &&
				0 == Txt.CompareOic(branch2.RemoteName, branch1.Name);
		}
	}
}