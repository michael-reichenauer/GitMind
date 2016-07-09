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
				SetMasterBranchCommits(repository,  master);
			}
		}


		public void SetNeighborCommitNames(MRepository repository)
		{
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
			IEnumerable<MSubBranch> lBranches = repository.SubBranches
				.Where(b => !b.Value.LatestCommit.HasBranchName).Select(b => b.Value);

			foreach (MSubBranch branch in lBranches)
			{
				MCommit commit = branch.LatestCommit;

				if (!commit.FirstChildren.Any())
				{
					commit.BranchName = branch.Name;
					commit.SubBranchId = branch.SubBranchId;
				}
			}
		}


		private static void SetMasterBranchCommits(MRepository repository, MSubBranch subBranch)
		{
			string commitId = subBranch.LatestCommitId;
			while (commitId != null)
			{
				MCommit commit = repository.Commits[commitId];

				if (commit.BranchName == subBranch.Name 
					&& commit.SubBranchId != null)
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


		private void SetEmptyParentCommits(MRepository repository)
		{
			// All commits, which do have a name, but first parent commit does not have a name
			IEnumerable<MCommit> commitsWithBranchName =
				repository.Commits
				.Where(commit =>
					commit.Value.HasBranchName
					&& commit.Value.HasFirstParent
					&& !commit.Value.FirstParent.HasBranchName)
				.Select(c => c.Value);

			foreach (MCommit xCommit in commitsWithBranchName)
			{
				string branchName = xCommit.BranchName;
				string subBranchId = xCommit.SubBranchId;

				MCommit last = xCommit;
				bool isFound = false;
				foreach (MCommit current in xCommit.FirstAncestors())
				{
					string currentBranchName = GetBranchName(current);

					if (current.HasBranchName && current.BranchName != branchName)
					{
						// found commit with branch name already set 
						break;
					}

					if (currentBranchName == branchName)
					{
						isFound = true;
						last = current;
					}
				}

				if (isFound)
				{
					foreach (MCommit current in xCommit.FirstAncestors())
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
		}

		private static void SetBranchCommitsOfParents(MRepository repository)
		{
			bool found;
			do
			{
				found = false;
				foreach (var commit in repository.Commits)
				{
					if (!commit.Value.HasBranchName && commit.Value.FirstChildren.Any())
					{
						MCommit firstChild = commit.Value.FirstChildren.ElementAt(0);
						if (firstChild.HasBranchName)
						{
							if (commit.Value.FirstChildren.All(c => c.BranchName == firstChild.BranchName))
							{
								commit.Value.BranchName = firstChild.BranchName;
								commit.Value.SubBranchId = firstChild.SubBranchId;
								found = true;
							}
						}
					}
				}
			} while (found);
		}
	}
}