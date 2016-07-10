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
			IEnumerable<MSubBranch> branches = repository.SubBranches.Values
				.Where(b => 
					b.LatestCommit.BranchId == null
					&& b.LatestCommit.SubBranchId == null);

			foreach (MSubBranch branch in branches)
			{
				MCommit commit = branch.LatestCommit;

				if (!commit.HasFirstChild)
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
				//if (commit.BranchId != null)
				//{
				//	break;
				//}

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


		private void SetEmptyParentCommits(MRepository repository)
		{
			// All commits, which do have a name, but first parent commit does not have a name
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

				MCommit last = commit;
				bool isFound = false;
				foreach (MCommit current in commit.FirstAncestors())
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