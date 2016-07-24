using System.Collections.Generic;
using System.Linq;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class CommitBranchNameService : ICommitBranchNameService
	{
		public void SetSpecifiedCommitBranchNames(
			IReadOnlyList<GitSpecifiedNames> specifiedNames,
			MRepository repository)
		{
			foreach (GitSpecifiedNames specifiedName in specifiedNames)
			{
				MCommit commit;
				if (repository.Commits.TryGetValue(specifiedName.CommitId, out commit))
				{
					commit.SpecifiedBranchName = specifiedName.BranchName;
					commit.BranchName = specifiedName.BranchName;
				}
			}
		}


		public void SetMasterBranchCommits(MRepository repository)
		{
			// Local master
			MSubBranch master = repository.SubBranches
				.FirstOrDefault(b => b.Value.Name == "master" && !b.Value.IsRemote).Value;
			if (master != null)
			{
				SetMasterBranchCommits(repository, master);
			}

			// Remote master
			master = repository.SubBranches
				.FirstOrDefault(b => b.Value.Name == "master" && b.Value.IsRemote).Value;
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




		public string GetBranchName(MCommit commit)
		{
			if (!string.IsNullOrEmpty(commit.BranchName))
			{
				return commit.BranchName;
			}
			else if (!string.IsNullOrEmpty(commit.SpecifiedBranchName))
			{
				return commit.SpecifiedBranchName;
			}
			else if (!string.IsNullOrEmpty(commit.FromSubjectBranchName))
			{
				return commit.FromSubjectBranchName;
			}
		
			return null;
		}


		public void SetBranchTipCommitsNames(MRepository repository)
		{
			IEnumerable<MSubBranch> branches = repository.SubBranches.Values
				.Where(b =>
					b.LatestCommit.BranchId == null
					&& b.LatestCommit.SubBranchId == null);

			foreach (MSubBranch branch in branches)
			{ 
				MCommit branchTip = branch.LatestCommit;

				if (!branchTip.HasFirstChild)
				{
					branchTip.BranchName = branch.Name;
					branchTip.SubBranchId = branch.SubBranchId;
				}
			}
		}


		private static void SetMasterBranchCommits(MRepository repository, MSubBranch subBranch)
		{
			string commitId = subBranch.LatestCommitId;
			while (commitId != null)
			{
				MCommit commit = repository.Commits[commitId];

				if (commit.BranchName == subBranch.Name && commit.SubBranchId != null)
				{
					// Do not break if commit is the tip
					if (!(commit.Id == subBranch.LatestCommitId && commit.SubBranchId == subBranch.SubBranchId))
					{
						break;
					}
				}

				if (commit.HasBranchName && commit.BranchName != subBranch.Name)
				{
					Log.Warn($"commit already has branch {commit.BranchName} != {subBranch.Name}");
					break;
				}

				commit.BranchName = subBranch.Name;
				commit.SubBranchId = subBranch.SubBranchId;
				commitId = commit.FirstParentId;
			}
		}


		private void SetActiveBranchCommits(MRepository repository)
		{
			IEnumerable<MSubBranch> branches = repository.SubBranches.Values
				.Where(b =>
					b.LatestCommit.BranchId == null
					&& b.IsActive);

			foreach (MSubBranch branch in branches)
			{
				MCommit branchTip = branch.LatestCommit;

				MCommit last = TryFindFirstAncestorWithSameName(branchTip, branch.Name);

				if (last == null)
				{
					// Could not find first ancestor commit with branch name 
					continue;
				}

				foreach (MCommit current in branchTip.CommitAndFirstAncestors())
				{
					current.BranchName = branch.Name;
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
						&& commit.SubBranchId != null
						&& commit.HasBranchName
						&& commit.HasFirstParent
						&& !commit.FirstParent.HasBranchName);

				foreach (MCommit commit in commitsWithBranchName)
				{
					string branchName = commit.BranchName;
					string subBranchId = commit.SubBranchId;

					MCommit last = TryFindFirstAncestorWithSameName(commit.FirstParent, branchName);

					if (last != null)
					{
						isFound = true;
						foreach (MCommit current in commit.FirstAncestors())
						{
							current.BranchName = branchName;
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


		private MCommit TryFindFirstAncestorWithSameName(MCommit startCommit, string branchName)
		{
			foreach (MCommit commit in startCommit.CommitAndFirstAncestors())
			{
				string commitBranchName = GetBranchName(commit);

				if (!string.IsNullOrEmpty(commitBranchName))			
				{
					if (commitBranchName == branchName)
					{
						return commit;
					}
					else
					{
						break;
					}
				}

				if (commit.BranchTipBranches.Count == 1 && commit.BranchTipBranches[0].Name == branchName)
				{
					return commit;
				}
			}

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
								commit.BranchName = firstChild.BranchName;
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