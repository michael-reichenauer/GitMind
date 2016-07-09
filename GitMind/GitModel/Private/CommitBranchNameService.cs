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
				if (repository.TryGetCommit(specifiedName.CommitId, out commit))
				{
					commit.SpecifiedBranchName = specifiedName.BranchName;
					commit.BranchName = specifiedName.BranchName;
				}
			}
		}


		public void SetMasterBranchCommits(IReadOnlyList<MSubBranch> branches, MRepository repository)
		{
			// Local master
			MSubBranch master = branches.FirstOrDefault(b => b.Name == "master" && !b.IsRemote);
			if (master != null)
			{
				SetBranchNameWithPriority(repository, master.LatestCommitId, master);
			}

			// Remote master
			master = branches.FirstOrDefault(b => b.Name == "master" && b.IsRemote);
			if (master != null)
			{
				SetBranchNameWithPriority(repository, master.LatestCommitId, master);
			}
		}


		public void SetNeighborCommitNames(MRepository repository)
		{
			SetEmptyParentCommits(repository.CommitList);

			SetBranchCommitsOfParents(repository.CommitList);
		}


		private static bool IsPullMergeCommit(MCommit commit)
		{
			return
				commit.HasSecondParent
				&& commit.BranchName != null
				&& commit.BranchName == commit.SecondParent.BranchName;
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


		public void SetBranchTipCommitsNames(IReadOnlyList<MSubBranch> branches, MRepository repository)
		{
			IEnumerable<MSubBranch> lBranches = branches.Where(b => !b.LatestCommit.HasBranchName);

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


		private static void SetBranchNameWithPriority(
			MRepository repository, string commitId, MSubBranch subBranch)
		{
			List<string> pullMergeTopCommits = new List<string>();

			while (commitId != null)
			{
				MCommit commit = repository.Commits(commitId);

				if (commit.BranchName == subBranch.Name && commit.SubBranchId != null)
				{
					break;
				}

				if (commit.HasBranchName && commit.BranchName != subBranch.Name)
				{
					Log.Warn($"commit already has branch {commit.BranchName} != {subBranch.Name}");
					break;
				}

				if (IsPullMergeCommit(commit))
				{
					pullMergeTopCommits.Add(commit.FirstParentId);
				}

				commit.BranchName = subBranch.Name;
				commit.SubBranchId = subBranch.SubBranchId;
				commitId = commit.FirstParentId;
			}

			pullMergeTopCommits.ForEach(id => SetBranchNameWithPriority(repository, id, subBranch));
		}


		private void SetEmptyParentCommits(IReadOnlyList<MCommit> commits)
		{
			// All commits, which do have a name, but first parent commit does not have a name
			IEnumerable<MCommit> commitsWithBranchName =
				commits.Where(commit =>
					commit.HasBranchName
					&& commit.HasFirstParent
					&& !commit.FirstParent.HasBranchName);

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

		private static void SetBranchCommitsOfParents(IReadOnlyList<MCommit> commits)
		{
			bool found;
			do
			{
				found = false;
				foreach (MCommit commit in commits)
				{
					if (!commit.HasBranchName && commit.FirstChildren.Any())
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