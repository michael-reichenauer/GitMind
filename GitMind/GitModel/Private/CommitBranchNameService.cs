using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Common;
using GitMind.Features.Commits.Private;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind.GitModel.Private
{
	internal class CommitBranchNameService : ICommitBranchNameService
	{
		public void SetSpecifiedCommitBranchNames(
			IReadOnlyList<CommitBranchName> specifiedNames,
			MRepository repository)
		{
			foreach (CommitBranchName specifiedName in specifiedNames)
			{
				if (TryGetCommit(repository, specifiedName.Id, out MCommit commit))
				{
					if (!string.IsNullOrEmpty(specifiedName.Name))
					{
						commit.SpecifiedBranchName = specifiedName.Name;
						commit.SetBranchName(specifiedName.Name);
					}
					else
					{
						commit.SpecifiedBranchName = null;
					}
				}
			}
		}


		public void SetCommitBranchNames(
			IReadOnlyList<CommitBranchName> commitBranches,
			MRepository repository)
		{
			foreach (CommitBranchName commitBranch in commitBranches)
			{
				if (TryGetCommit(repository, commitBranch.Id, out MCommit commit))
				{
					// Set branch name unless there is a specified branch name which has higher priority
					commit.CommitBranchName = commitBranch.Name;

					if (string.IsNullOrEmpty(commit.SpecifiedBranchName))
					{
						commit.SetBranchName(commitBranch.Name);
					}
				}
			}
		}


		public void SetMasterBranchCommits(MRepository repository)
		{
			// Local master
			MSubBranch master = repository.SubBranches
				.FirstOrDefault(b => b.Value.Name == BranchName.Master && !b.Value.IsRemote).Value;
			if (master != null)
			{
				SetMasterBranchCommits(repository, master);
			}

			// Remote master
			master = repository.SubBranches
				.FirstOrDefault(b => b.Value.Name == BranchName.Master && b.Value.IsRemote).Value;
			if (master != null)
			{
				SetMasterBranchCommits(repository, master);
			}
		}


		public void SetNeighborCommitNames(MRepository repository)
		{
			SetActiveBranchCommits(repository);

			SetEmptyParentCommits(repository);

			SetBranchCommitsOfParents(repository);
		}


		public BranchName GetBranchName(MCommit commit)
		{
			if (commit.BranchName != null)
			{
				return commit.BranchName;
			}
			else if (commit.SpecifiedBranchName != null)
			{
				return commit.SpecifiedBranchName;
			}
			else if (commit.FromSubjectBranchName != null)
			{
				return commit.FromSubjectBranchName;
			}

			return null;
		}


		public void SetBranchTipCommitsNames(MRepository repository)
		{
			IEnumerable<MSubBranch> branches = repository.SubBranches.Values
				.Where(b =>
					b.TipCommit.BranchId == null
					&& b.TipCommit.SubBranchId == null);

			foreach (MSubBranch branch in branches)
			{
				MCommit branchTip = branch.TipCommit;

				if (!branchTip.HasFirstChild
					&& !branches.Any(b => b.Name != branch.Name && b.TipCommitId == branch.TipCommitId))
				{
					branchTip.SetBranchName(branch.Name);
					branchTip.SubBranchId = branch.SubBranchId;
				}
			}
		}


		private static bool TryGetCommit(
			MRepository repository,
			string id,
			out MCommit commit)
		{
			if (CommitId.TryParse(id, out CommitId commitId))
			{
				return repository.Commits.TryGetValue(commitId, out commit);
			}
			else
			{
				foreach (var pair in repository.GitCommits.ToList())
				{
					if (pair.Value.Sha.Sha.StartsWithOic(id))
					{
						commitId = new CommitId(pair.Value.Sha);
						return repository.Commits.TryGetValue(commitId, out commit);
					}
				}
			}

			commit = null;
			return false;
		}


		private static void SetMasterBranchCommits(MRepository repository, MSubBranch subBranch)
		{
			CommitId commitId = subBranch.TipCommitId;
			while (commitId != CommitId.None)
			{
				MCommit commit = repository.Commits[commitId];

				if (commit.BranchName == subBranch.Name && commit.SubBranchId != null)
				{
					// Do not break if commit is the tip
					if (!(commit.Id == subBranch.TipCommitId && commit.SubBranchId == subBranch.SubBranchId))
					{
						break;
					}
				}

				if (commit.HasBranchName && commit.BranchName != subBranch.Name)
				{
					Log.Warn($"commit {commit} already has branch {commit.BranchName} != {subBranch.Name}");
					break;
				}

				commit.SetBranchName(subBranch.Name);
				commit.SubBranchId = subBranch.SubBranchId;
				commitId = commit.FirstParentId;
			}
		}


		private void SetActiveBranchCommits(MRepository repository)
		{
			IEnumerable<MSubBranch> branches = repository.SubBranches.Values
				.Where(b =>
					b.TipCommit.BranchId == null
					&& b.IsActive);

			foreach (MSubBranch branch in branches)
			{
				MCommit branchTip = branch.TipCommit;

				MCommit last = TryFindFirstAncestorWithSameName(branchTip, branch.Name);

				if (last == null)
				{
					// Could not find first ancestor commit with branch name 
					continue;
				}

				foreach (MCommit current in branchTip.CommitAndFirstAncestors())
				{
					current.SetBranchName(branch.Name);
					current.SubBranchId = branch.SubBranchId;

					if (current == last)
					{
						break;
					}
				}
			}
		}


		private void SetEmptyParentCommits(MRepository repository)
		{
			// All commits, which do have a name, but first parent commit does not have a name
			bool isFound;
			do
			{
				isFound = false;
				IEnumerable<MCommit> commitsWithBranchName = repository.Commits.Values
					.Where(commit =>
						commit.BranchId == null
						&& commit.HasBranchName
						&& commit.HasFirstParent
						&& !commit.FirstParent.HasBranchName);

				foreach (MCommit commit in commitsWithBranchName)
				{
					BranchName branchName = commit.BranchName;
					string subBranchId = commit.SubBranchId;

					MCommit last = TryFindFirstAncestorWithSameName(commit.FirstParent, branchName);

					if (last != null)
					{
						isFound = true;
						foreach (MCommit current in commit.FirstAncestors())
						{
							current.SetBranchName(branchName);
							current.SubBranchId = subBranchId;

							if (current == last)
							{
								break;
							}
						}
					}
				}
			} while (isFound);
		}


		private MCommit TryFindFirstAncestorWithSameName(MCommit startCommit, BranchName branchName)
		{
			foreach (MCommit commit in startCommit.CommitAndFirstAncestors())
			{
				BranchName commitBranchName = GetBranchName(commit);

				if (commitBranchName != null)
				{
					if (commitBranchName == branchName)
					{
						// Found an ancestor, which has branch name we are searching fore
						return commit;
					}
					else
					{
						// Fond an ancestor with another different name
						break;
					}
				}

				if (commit != startCommit
					&& commit.BranchTipBranches.Count == 1 && commit.BranchTipBranches[0].Name == branchName)
				{
					// Found a commit with a branch tip of a branch with same name,
					// this can happen for local/remote pairs. Lets assume the commit is the on that branch
					return commit;
				}
			}

			// Could not find an ancestor with the branch name we a searching for
			return null;
		}


		private static void SetBranchCommitsOfParents(MRepository repository)
		{
			bool found;
			do
			{
				found = false;
				foreach (var commit in repository.Commits.Values)
				{
					if (commit.BranchId == null && !commit.HasBranchName && commit.HasFirstChild)
					{
						MCommit firstChild = commit.FirstChildren.ElementAt(0);
						if (firstChild.HasBranchName)
						{
							if (commit.FirstChildren.All(c => c.BranchName == firstChild.BranchName))
							{
								commit.SetBranchName(firstChild.BranchName);
								commit.SubBranchId = firstChild.SubBranchId;
								found = true;
							}
						}
					}
				}
			} while (found);
		}
	}
}