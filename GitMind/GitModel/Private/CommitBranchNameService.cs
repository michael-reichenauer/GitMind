using System.Collections.Generic;
using System.Linq;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class CommitBranchNameService : ICommitBranchNameService
	{
		public void SetCommitBranchNames(
			IReadOnlyList<MCommit> commits,
			IReadOnlyList<SpecifiedBranchName> specifiedBranches,
			MRepository repository)
	{
			Timing t = new Timing();
			SetSpecifiedCommitBranchNames(specifiedBranches, repository);
			t.Log("Set specified branch names");

			SetSubjectCommitBranchNames(commits, repository);
			t.Log("Parse subject branch names");
		}


		public void SetCommitBranchNames(
			IReadOnlyList<MSubBranch> branches,
			IReadOnlyList<MCommit> commits,
			MRepository repository)
		{
			Timing t = new Timing();
			SetMasterBranchCommits(branches, repository);
			t.Log("Set master branch commits");

			SetBranchCommits(branches, repository);
			t.Log("Set branch commits");

			SetEmptyParentCommits(commits);
			t.Log("Set empty parent commits");

			SetBranchCommitsOfParents(commits);
			t.Log("Set same branch name as parent with name");
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


		private void SetSpecifiedCommitBranchNames(
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

		private void SetSubjectCommitBranchNames(
			IReadOnlyList<MCommit> commits, MRepository repository)
		{
			foreach (MCommit xCommit in commits)
			{
				xCommit.BranchNameFromSubject = TryExtractBranchNameFromSubject(xCommit, repository);
			}
		}


		public void SetBranchCommits(IReadOnlyList<MSubBranch> branches, MRepository repository)
		{
			foreach (MSubBranch xBranch in branches.ToList())
			{
				string id = xBranch.LatestCommitId;
				SetBranchName(repository, id, xBranch);
			}
		}

		private void SetMasterBranchCommits(IReadOnlyList<MSubBranch> branches, MRepository repository)
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


		private static void SetBranchNameWithPriority(
			MRepository repository, string commitId, MSubBranch subBranch)
		{
			while (commitId != null)
			{
				MCommit commit = repository.Commits[commitId];

				if (commit.BranchXName == subBranch.Name)
				{
					break;
				}

				commit.BranchXName = subBranch.Name;
				commit.SubBranchId = subBranch.Id;
				commitId = commit.FirstParentId;
			}
		}

		private void SetBranchName(MRepository repository, string commitId, MSubBranch subBranch)
		{
			if (string.IsNullOrEmpty(commitId))
			{
				return;
			}

			foreach (MSubBranch b in repository.SubBranches)
			{
				if (b.Name != subBranch.Name
						&& !(subBranch.IsActive && !b.IsActive)
						&& !(subBranch.IsMultiBranch)
						&& (b.LatestCommitId == commitId))
				{
					MCommit commit = repository.Commits[commitId];
					//Log.Warn($"Commit {commit} in branch {subBranch} same as other branch {b}");
					return;
				}
			}


			string currentId = commitId;
			while (currentId != null)
			{
				MCommit commit = repository.Commits[currentId];

				if (!string.IsNullOrEmpty(commit.BranchXName))
				{
					break;
				}

				string branchName = GetBranchName(commit);
				if (branchName != subBranch.Name &&
						!(subBranch.IsMultiBranch && currentId == commitId))
				{
					// for multi branches, first commit is a branch root
					if (commit.ChildIds.Count > 1)
					{
						if (0 != commit.FirstChildren.Count(
							child => GetBranchName(child) != subBranch.Name))
						{
							//Log.Warn($"Found commit which belongs to multiple different branches: {xCommit}");
							break;
						}

						if (0 != repository.SubBranches.Count(b => b != subBranch && b.LatestCommit == commit))
						{
							break;
						}
					}
				}

				commit.BranchXName = subBranch.Name;
				commit.SubBranchId = subBranch.Id;
				currentId = commit.FirstParentId;
			}
		}

	

		private bool IsPullMergeCommit(MCommit commit, MSubBranch subBranch)
		{
			return
				commit.HasSecondParent
				&& (commit.MergeSourceBranchNameFromSubject == subBranch.Name
						|| GetBranchName(commit.SecondParent) == subBranch.Name);
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

					//if (string.IsNullOrEmpty(currentBranchName) || currentBranchName == branchName)
					//{

					//}

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
			IEnumerable<MCommit> commitsWithBranchName =
				commits.Where(commit =>
					commit.HasBranchName
					&& commit.HasFirstParent
					&& !commit.FirstParent.HasBranchName);

			foreach (MCommit xCommit in commitsWithBranchName)
			{
				string branchName = xCommit.BranchXName;
				string subBranchId = xCommit.SubBranchId;

				foreach (MCommit current in xCommit.FirstAncestors()
					.TakeWhile(c => c.FirstChildIds.Count <= 1 && !c.HasBranchName))
				{
					current.BranchXName = branchName;
					current.SubBranchId = subBranchId;
				}
			}
		}


		private string TryExtractBranchNameFromSubject(MCommit commit, MRepository repository)
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