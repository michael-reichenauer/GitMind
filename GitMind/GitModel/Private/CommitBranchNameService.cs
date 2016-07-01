using System.Collections.Generic;
using System.Linq;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class CommitBranchNameService : ICommitBranchNameService
	{
		public void SetSpecifiedCommitBranchNames(
			IReadOnlyList<SpecifiedBranchName> specifiedNames,
			MRepository repository)
		{
			foreach (SpecifiedBranchName specifiedName in specifiedNames)
			{
				MCommit commit;
				if (repository.Commits.TryGetValue(specifiedName.CommitId, out commit))
				{
					commit.BranchNameSpecified = specifiedName.BranchName;
					commit.BranchXName = specifiedName.BranchName;
				}
			}
		}


		public void SetPullMergeCommitBranchNames(IReadOnlyList<MCommit> commits)
		{
			IEnumerable<MCommit> pullMergeCommits = commits.Where(IsPullMergeCommit);

			foreach (MCommit commit in pullMergeCommits)
			{
				if (!commit.HasBranchName)
				{
					commit.BranchXName = commit.MergeSourceBranchNameFromSubject;
					if (!commit.SecondParent.HasBranchName)
					{
						commit.SecondParent.BranchXName = commit.MergeSourceBranchNameFromSubject;
					}
				}
			}
		}


		public void SetSubjectCommitBranchNames(IReadOnlyList<MCommit> commits, MRepository repository)
		{
			foreach (MCommit commit in commits)
			{
				commit.BranchNameFromSubject = TryExtractBranchNameFromSubject(commit, repository);
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


		public void SetNeighborCommitNames(IReadOnlyList<MCommit> commits)
		{
			SetEmptyParentCommits(commits);

			SetBranchCommitsOfParents(commits);
		}

		
		private static bool IsPullMergeCommit(MCommit commit)
		{
			return
				commit.HasSecondParent
				&& commit.MergeSourceBranchNameFromSubject != null
				&& commit.MergeSourceBranchNameFromSubject == commit.MergeTargetBranchNameFromSubject;
		}


		public string GetBranchName(MCommit commit)
		{
			if (!string.IsNullOrEmpty(commit.BranchXName))
			{
				return commit.BranchXName;
			}
			else if (!string.IsNullOrEmpty(commit.BranchNameSpecified))
			{
				return commit.BranchNameSpecified;
			}
			else if (!string.IsNullOrEmpty(commit.BranchNameFromSubject))
			{
				return commit.BranchNameFromSubject;
			}

			return null;
		}


		public void SetBranchTipCommitsNames(IReadOnlyList<MSubBranch> branches, MRepository repository)
		{
			IEnumerable<MSubBranch> lBranches = branches.Where(b => !b.LatestCommit.HasBranchName);

			foreach (MSubBranch branch in lBranches)
			{
				MCommit commit = branch.LatestCommit;

				commit.BranchXName = branch.Name;
				commit.SubBranchId = branch.SubBranchId;		
			}
		}


		private static void SetBranchNameWithPriority(
			MRepository repository, string commitId, MSubBranch subBranch)
		{
			List<string> pullMergeTopCommits = new List<string>();

			while (commitId != null)
			{
				MCommit commit = repository.Commits[commitId];

				if (commit.BranchXName == subBranch.Name && commit.SubBranchId != null)
				{
					break;
				}

				if (commit.HasBranchName && commit.BranchXName != subBranch.Name)
				{
					Log.Warn($"commit already has branch {commit.BranchXName} != {subBranch.Name}");
					break;
				}

				if (IsPullMergeCommit(commit))
				{
					pullMergeTopCommits.Add(commit.FirstParentId);
				}

				commit.BranchXName = subBranch.Name;
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
				string branchName = xCommit.BranchXName;
				string subBranchId = xCommit.SubBranchId;

				MCommit last = xCommit;
				bool isFound = false;
				foreach (MCommit current in xCommit.FirstAncestors())
				{
					string currentBranchName = GetBranchName(current);

					if (current.HasBranchName && current.BranchXName != branchName)
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
						current.BranchXName = branchName;
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
							if (commit.FirstChildren.All(c => c.BranchXName == firstChild.BranchXName))
							{
								commit.BranchXName = firstChild.BranchXName;
								commit.SubBranchId = firstChild.SubBranchId;
								found = true;
							}
						}
					}
				}
			} while (found);
		}


		private static string TryExtractBranchNameFromSubject(MCommit commit, MRepository repository)
		{
			if (commit.SecondParentId != null)
			{
				// This is a merge commit, and the subject of this commit might contain the
				// target (this current) branch  name in the subject like e.g.:
				// "Merge <source-branch> into <target-branch>"
				string targetBranchName = commit.MergeTargetBranchNameFromSubject;
				if (targetBranchName != null)
				{
					return targetBranchName;
				}
			}

			// If a child of this commit is a merge commit merged from this commit, lets try to get
			// the source branch name of that commit. I.e. a child commit might have a subject like e.g.:
			// "Merge <source-branch> into <target-branch>"
			// That source branch name would thus be the name of the branch of this commit.
			foreach (string childId in commit.ChildIds)
			{
				MCommit child = repository.Commits[childId];
				if (child.SecondParentId == commit.Id
					&& !string.IsNullOrEmpty(child.MergeSourceBranchNameFromSubject))
				{
					// Found a child with a source branch name
					return child.MergeSourceBranchNameFromSubject;
				}
			}

			return null;
		}
	}
}