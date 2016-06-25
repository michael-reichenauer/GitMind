using System.Collections.Generic;
using System.Linq;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class CommitBranchNameService : ICommitBranchNameService
	{
		public void SetCommitBranchNames(
				IReadOnlyList<MCommit> commits,
				IReadOnlyList<SpecifiedBranch> specifiedBranches,
				MRepository mRepository)
		{
			Timing t = new Timing();
			SetSpecifiedCommitBranchNames(specifiedBranches, mRepository);
			t.Log("Set specified branch names");

			SetSubjectCommitBranchNames(commits, mRepository);
			t.Log("Parse subject branch names");
		}


		public void SetCommitBranchNames(
			IReadOnlyList<MSubBranch> branches,
			IReadOnlyList<MCommit> commits,
			MRepository mRepository)
		{
			Timing t = new Timing();
			SetMasterBranchCommits(branches, mRepository);
			t.Log("Set master branch commits");

			SetBranchCommits(branches, mRepository);
			t.Log("Set branch commits");

			SetEmptyParentCommits(commits);
			t.Log("Set empty parent commits");

			SetBranchCommitsOfParents(commits);
			t.Log("Set same branch name as parent with name");
		}


		private void SetSpecifiedCommitBranchNames(
			IReadOnlyList<SpecifiedBranch> commitBranches,
			MRepository xmodel)
		{
			foreach (SpecifiedBranch commitBranch in commitBranches)
			{
				MCommit mCommit;
				if (xmodel.Commits.TryGetValue(commitBranch.CommitId, out mCommit))
				{
					mCommit.BranchNameSpecified = commitBranch.BranchName;
					mCommit.BranchXName = commitBranch.BranchName;
					mCommit.SubBranchId = commitBranch.SubBranchId;
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


		private string TryExtractBranchNameFromSubject(MCommit mCommit, MRepository mRepository)
		{
			if (mCommit.SecondParentId != null)
			{
				// This is a merge commit, and the subject might contain the target (this current) branch 
				// name in the subject like e.g. "Merge <source-branch> into <target-branch>"
				string branchName = mCommit.MergeTargetBranchNameFromSubject;
				if (branchName != null)
				{
					return branchName;
				}
			}

			// If a child of this commit is a merge commit merged from this commit, lets try to get
			// the source branch name of that commit. I.e. a child commit might have a subject like
			// e.g. "Merge <source-branch> ..." That source branch would thus be the name of the branch
			// of this commit.
			foreach (string childId in mCommit.ChildIds)
			{
				MCommit child = mRepository.Commits[childId];
				if (child.SecondParentId == mCommit.Id
						&& !string.IsNullOrEmpty(child.MergeSourceBranchNameFromSubject))
				{
					return child.MergeSourceBranchNameFromSubject;
				}
			}

			return null;
		}



		public string GetBranchName(MCommit mCommit)
		{
			if (!string.IsNullOrEmpty(mCommit.BranchXName))
			{
				return mCommit.BranchXName;
			}
			else if (!string.IsNullOrEmpty(mCommit.BranchNameSpecified))
			{
				return mCommit.BranchNameSpecified;
			}
			else if (!string.IsNullOrEmpty(mCommit.BranchNameFromSubject))
			{
				return mCommit.BranchNameFromSubject;
			}

			return null;
		}


		public void SetBranchCommits(IReadOnlyList<MSubBranch> branches, MRepository xmodel)
		{
			foreach (MSubBranch xBranch in branches.ToList())
			{
				string id = xBranch.LatestCommitId;
				SetBranchName(xmodel, id, xBranch);
			}
		}


		private void SetBranchName(MRepository xmodel, string id, MSubBranch subBranch)
		{
			if (string.IsNullOrEmpty(id))
			{
				return;
			}

			if (xmodel.Commits[id].ShortId == "afe62f")
			{

			}

			foreach (MSubBranch b in xmodel.SubBranches)
			{
				if (b.Name != subBranch.Name
						&& !(subBranch.IsActive && !b.IsActive)
						&& !(subBranch.IsMultiBranch)
						&& (b.LatestCommitId == id))
				{
					MCommit c = xmodel.Commits[id];
					//Log.Warn($"Commit {c} in branch {xBranch} same as other branch {b}");
					return;
				}
			}

			List<MCommit> pullmerges = new List<MCommit>();

			string currentId = id;
			while (true)
			{
				if (currentId == null)
				{
					break;
				}

				MCommit mCommit = xmodel.Commits[currentId];

				if (!string.IsNullOrEmpty(mCommit.BranchXName))
				{
					break;
				}

				if (IsPullMergeCommit(mCommit, subBranch))
				{
					pullmerges.Add(mCommit);

				}

				if (GetBranchName(mCommit) != subBranch.Name &&
						!(subBranch.IsMultiBranch && currentId == id))
				{
					// for multi branches, first commit is a branch root
					if (mCommit.ChildIds.Count > 1)
					{
						if (0 != mCommit.FirstChildren.Count(child => GetBranchName(child) != subBranch.Name))
						{
							//Log.Warn($"Found commit which belongs to multiple different branches: {xCommit}");
							break;
						}

						if (0 != xmodel.SubBranches.Count(b => b != subBranch && b.LatestCommit == mCommit))
						{
							break;
						}
					}
				}

				mCommit.BranchXName = subBranch.Name;
				mCommit.SubBranchId = subBranch.Id;

				currentId = mCommit.FirstParentId;
			}

			foreach (MCommit xCommit in pullmerges)
			{
				//SetBranchName(xmodel, xCommit.SecondParentId, subBranch);

				//RemovePullMergeBranch(xmodel, xBranch, xCommit.SecondParentId);
			}
		}

		private void SetMasterBranchCommits(IReadOnlyList<MSubBranch> branches, MRepository xmodel)
		{
			// Local master
			MSubBranch master = branches.FirstOrDefault(b => b.Name == "master" && !b.IsRemote);
			if (master != null)
			{
				SetBranchNameWithPriority(xmodel, master.LatestCommitId, master);
			}

			// Remote master
			master = branches.FirstOrDefault(b => b.Name == "master" && b.IsRemote);
			if (master != null)
			{
				SetBranchNameWithPriority(xmodel, master.LatestCommitId, master);
			}
		}


		private void SetBranchNameWithPriority(
			MRepository xmodel, string id, MSubBranch subBranch)
		{
			List<MCommit> pullmerges = new List<MCommit>();

			while (true)
			{
				if (id == null)
				{
					break;
				}

				MCommit mCommit = xmodel.Commits[id];

				if (mCommit.BranchXName == subBranch.Name)
				{
					break;
				}

				if (IsPullMergeCommit(mCommit, subBranch))
				{
					pullmerges.Add(mCommit);
				}

				if (!string.IsNullOrEmpty(mCommit.BranchNameFromSubject) &&
						mCommit.BranchNameFromSubject != subBranch.Name)
				{
					//Log.Debug($"Setting different name '{xBranch.Name}'!='{xCommit.BranchNameFromSubject}'");
				}

				mCommit.BranchXName = subBranch.Name;
				mCommit.SubBranchId = subBranch.Id;


				id = mCommit.FirstParentId;
			}

			foreach (MCommit xCommit in pullmerges)
			{
				//SetBranchNameWithPriority(xmodel, xCommit.SecondParentId, subBranch);
				//RemovePullMergeBranch(xmodel, xBranch, xCommit.SecondParentId);
			}
		}

		private bool IsPullMergeCommit(MCommit mCommit, MSubBranch subBranch)
		{
			return
				mCommit.HasSecondParent
				&& (mCommit.MergeSourceBranchNameFromSubject == subBranch.Name
						|| GetBranchName(mCommit.SecondParent) == subBranch.Name);
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
	}
}