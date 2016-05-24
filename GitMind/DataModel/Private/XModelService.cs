using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.DataModel.Private
{
	internal class XModelService : IXModelService
	{
		public MModel XGetModel(IGitRepo gitRepo)
		{
			IReadOnlyList<GitCommit> gitCommits = gitRepo.GetAllCommts().ToList();
			IReadOnlyList<GitBranch> gitBranches = gitRepo.GetAllBranches();
			IReadOnlyList<CommitBranch> commitBranches = new CommitBranch[0];

			MModel mModel = new MModel();
			Timing t = new Timing();
			IReadOnlyList<MCommit> commits = AddCommits(gitCommits, mModel);
			t.Log("added commits");

			SetChildren(commits);
			t.Log("Set children");

			SetSpecifiedCommitBranchNames(commitBranches, mModel);
			t.Log("Set specified branch names");

			SetSubjectCommitBranchNames(commits, mModel);
			t.Log("Parse subject branch names");

			IReadOnlyList<MSubBranch> branches1 = AddActiveBranches(gitBranches, mModel);
			t.Log("Add branches");
			Log.Debug($"Number of active branches {branches1.Count} ({mModel.SubBranches.Count})");

			IReadOnlyList<MSubBranch> branches2 = AddMergedInactiveBranches(commits, mModel);
			IReadOnlyList<MSubBranch> branches = branches1.Concat(branches2).ToList();
			t.Log("Add merged branches");
			Log.Debug($"Number of inactive branches {branches2.Count} ({mModel.SubBranches.Count})");
			//branches2.ForEach(b => Log.Debug($"   Branch {b}"));

			SetMasterBranchCommits(branches, mModel);
			t.Log("Set master branch commits");
			Log.Debug($"Unset commits {commits.Count(c => !c.HasBranchName)}");
			Log.Debug($"Number of branches {mModel.SubBranches.Count}");

			SetBranchCommits(branches, mModel);
			t.Log("Set branch commits");
			Log.Debug($"Unset commits {commits.Count(c => !c.HasBranchName)}");
		
			SetEmptyParentCommits(commits);
			t.Log("Set empty parent commits");
			Log.Debug($"Unset commits {commits.Count(c => !c.HasBranchName)}");

			SetBranchCommitsOfParents(commits);
			t.Log("Set same branch name as parent with name");
			Log.Debug($"Unset commits {commits.Count(c => !c.HasBranchName)}");

			IReadOnlyList<MSubBranch> branches3 = AddMultiBranches(commits, branches, mModel);
			t.Log("Add multi branches");
			Log.Debug($"Number of multi branches {branches3.Count} ({mModel.SubBranches.Count})");
			SetBranchCommits(branches3, mModel);

			branches = branches.Concat(branches3).ToList();

			Log.Debug($"Unset commits after multi {commits.Count(c => !c.HasBranchName)}");
			commits.Where(c => string.IsNullOrEmpty(c.BranchName))
				.ForEach(c => Log.Warn($"   Unset {c} -> parent: {c.FirstParentId}"));
			Log.Debug($"All branches ({branches.Count})");

			SetParentCommitId(branches);
			GroupSubBranches(branches);

			SetBranchHierarchy(mModel.Branches);

			Log.Debug($"Number of total branches {mModel.Branches.Count}");
			Log.Debug($"Number of total commits {mModel.Commits.Count}");
			Log.Debug($"Number of IsAnonymous branches {mModel.Branches.Count(b => b.IsAnonymous && !b.IsMultiBranch)}");
			return mModel;
		}


		private void SetBranchHierarchy(IReadOnlyList<MBranch> branches)
		{

			
			foreach (MBranch xBranch in branches)
			{
				if (xBranch.ParentCommitId != null && xBranch.ParentCommit.BranchId != xBranch.Id)
				{
					xBranch.ParentBranchId = xBranch.ParentCommit.BranchId;

					MBranch parentBranch = xBranch.ParentBranch;
					if (!parentBranch.ChildBranches.Contains(xBranch))
					{
						parentBranch.ChildBranches.Add(xBranch);
					}
				}
				else
				{
					Log.Debug($"Branch {xBranch} has no parent branch");
				}
			}

			//foreach (XBranch xBranch in branches.Where(b => b.ParentBranchId == null))
			//{
			//	LogBranchHierarchy(xBranch, 0);
			//}
		}


		private void LogBranchHierarchy(MBranch mBranch, int indent)
		{
			string indentText = new string(' ', indent);
			Log.Debug($"{indentText}{mBranch}");

			foreach (MBranch childBranch in mBranch.ChildBranches.OrderBy(b => b.Name))
			{
				LogBranchHierarchy(childBranch, indent + 3);
			}
		}


		private static void SetParentCommitId(IReadOnlyList<MSubBranch> subBranches)
		{
			foreach (MSubBranch subBranch in subBranches)
			{
				MCommit LatestCommit = subBranch.LatestCommit;

				IEnumerable<MCommit> commits = subBranch.LatestCommit.FirstAncestors()
					.TakeWhile(c => c.BranchName == subBranch.Name);

				if (commits.Any())
				{
					MCommit firstCommit = commits.Last();
					subBranch.FirstCommitId = firstCommit.Id;
					subBranch.ParentCommitId = firstCommit.FirstParentId;
				}
				else
				{
					if (LatestCommit.BranchName != null)
					{
						subBranch.FirstCommitId = LatestCommit.Id;
						subBranch.ParentCommitId = LatestCommit.FirstParentId;
					}
					else
					{
						Log.Warn($"Branch with no commits {subBranch}");
					}
				}
			}	
		}

		private static void GroupSubBranches(IReadOnlyList<MSubBranch> branches)
		{
			var groupedOnName = branches.GroupBy(b => b.Name);

			foreach (var groupByName in groupedOnName)
			{
				var groupedByParentCommitId = groupByName.GroupBy(b => b.ParentCommitId);

				foreach (var groupByBranch in groupedByParentCommitId)
				{
					string id = Guid.NewGuid().ToString();
					MSubBranch subBranch = groupByBranch.First();
					MBranch mBranch = new MBranch(subBranch.MModel)
					{
						Id = id,
						Name = subBranch.Name,
						IsMultiBranch = subBranch.IsMultiBranch,
						IsActive = subBranch.IsActive,
						IsAnonymous = subBranch.IsAnonymous,
						ParentCommitId = subBranch.ParentCommitId
					};

					mBranch.SubBranches.AddRange(groupByBranch);
					mBranch.SubBranches.ForEach(b => b.BranchId = id);

					mBranch.Commits.AddRange(
						groupByBranch
							.SelectMany(branch =>
								new[] { branch.LatestCommit }
									.Where(c => c.SubBranchId == branch.Id && c.Id != branch.ParentCommitId) 
								.Concat(
									branch.LatestCommit
										.FirstAncestors()
										.TakeWhile(c => c.SubBranchId == branch.Id && c.Id != branch.ParentCommitId)))
								.Distinct()
							.OrderBy(c => c.CommitDate));

					if (mBranch.Commits.Any(c => c.BranchId != null))
					{
						var x = mBranch.Commits.Where(c => c.BranchId != null).ToList();
						Log.Error($"Commits belong to multiple branches {mBranch}");
					}

					mBranch.Commits.ForEach(c => c.BranchId = id);

					mBranch.MModel.Branches.Add(mBranch);
				}
			}
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
					if (current.HasBranchName && current.BranchName != branchName)
					{
						// found commit with branch name already set 
						break;
					}

					string currentBranchName = GetBranchName(current);
					if (string.IsNullOrEmpty(currentBranchName) || currentBranchName == branchName)
					{
						last = current;
					}

					if (currentBranchName == branchName)
					{
						isFound = true;
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
			IEnumerable<MCommit> commitsWithBranchName =
				commits.Where(commit =>
					commit.HasBranchName
					&& commit.HasFirstParent
					&& !commit.FirstParent.HasBranchName);

			foreach (MCommit xCommit in commitsWithBranchName)
			{
				string branchName = xCommit.BranchName;
				string subBranchId = xCommit.SubBranchId;

				foreach (MCommit current in xCommit.FirstAncestors()
					.TakeWhile(c => c.FirstChildIds.Count <= 1 && !c.HasBranchName))
				{
					current.BranchName = branchName;
					current.SubBranchId = subBranchId;

				}
			}
		}


		private IReadOnlyList<MSubBranch> AddMultiBranches(
			IReadOnlyList<MCommit> commits, IReadOnlyList<MSubBranch> branches, MModel xmodel)
		{
			IEnumerable<MCommit> roots =
				commits.Where(c =>
				string.IsNullOrEmpty(c.BranchName)
				&& c.FirstChildIds.Count > 1);

			// The commits where multiple branches are starting and the commits has no branch name
			IEnumerable<MCommit> roots2 = branches
				.GroupBy(b => b.LatestCommitId)
				.Where(group => group.Count() > 1)
				.Select(group => xmodel.Commits[group.Key])
				.Where(c => string.IsNullOrEmpty(c.BranchName));

			roots = roots.Concat(roots2);

			List<MSubBranch> multiBranches = new List<MSubBranch>();
			foreach (MCommit root in roots)
			{
				string branchName = "Multibranch_" + root.ShortId;

				MSubBranch subBranch = new MSubBranch(xmodel)
				{
					Id = Guid.NewGuid().ToString(),
					Name = branchName,		
					LatestCommitId = root.Id,
					IsMultiBranch = true,
					IsActive = false,
					IsAnonymous = true
				};

				xmodel.SubBranches.Add(subBranch);
				multiBranches.Add(subBranch);
			}

			return multiBranches;
		}


		private IReadOnlyList<MSubBranch> AddMergedInactiveBranches(
			IReadOnlyList<MCommit> commits, MModel mModel)
		{
			List<MSubBranch> branches = new List<MSubBranch>();

			// Commits which has no child, which has this commit as a first parent, i.e. it is the 
			// top of a branch and there is no existing branch at this commit
			IEnumerable<MCommit> topCommits = commits.Where(commit =>
				!commit.FirstChildIds.Any()
				&& !mModel.SubBranches.Any(b =>b.LatestCommitId == commit.Id));

			IEnumerable<MCommit> pullMergeTopCommits = commits
				.Where(commit =>
					commit.HasSecondParent
					&& commit.MergeSourceBranchNameFromSubject != null
					&& commit.MergeSourceBranchNameFromSubject == commit.MergeTargetBranchNameFromSubject)
				.Select(c => c.SecondParent);

			topCommits = topCommits.Concat(pullMergeTopCommits).Distinct();


			foreach (MCommit xCommit in topCommits)
			{
				MSubBranch subBranch = new MSubBranch(mModel)
				{
					Id = Guid.NewGuid().ToString(),
					LatestCommitId = xCommit.Id,
					IsMultiBranch = false,
					IsActive = false
				};

				string branchName = TryFindBranchName(xCommit);
				if (string.IsNullOrEmpty(branchName))
				{
					branchName = "Branch_" + xCommit.ShortId;
					subBranch.IsAnonymous = true;
				}

				subBranch.Name = branchName;

				mModel.SubBranches.Add(subBranch);
				branches.Add(subBranch);
			}


			return branches;
		}


		private string TryFindBranchName(MCommit mCommit)
		{
			string branchName = GetBranchName(mCommit);

			if (branchName == null)
			{
				int count = 0;
				// Could not find a branch name from the commit, lets try it ancestors
				foreach (MCommit commit in mCommit.FirstAncestors()
					.TakeWhile(c => c.HasSingleFirstChild))
				{
					count++;
					string name = GetBranchName(commit);
					if (name != null)
					{
						return name;
					}
				}
			}

			return branchName;
		}


		private void SetBranchCommits(IReadOnlyList<MSubBranch> branches, MModel xmodel)
		{
			foreach (MSubBranch xBranch in branches.ToList())
			{
				string id = xBranch.LatestCommitId;
				SetBranchName(xmodel, id, xBranch);
			}
		}


		private void SetBranchName(MModel xmodel, string id, MSubBranch subBranch)
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
					&& ( b.LatestCommitId == id))
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

				if (!string.IsNullOrEmpty(mCommit.BranchName))
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

				mCommit.BranchName = subBranch.Name;
				mCommit.SubBranchId = subBranch.Id;

				currentId = mCommit.FirstParentId;
			}

			foreach (MCommit xCommit in pullmerges)
			{
				//SetBranchName(xmodel, xCommit.SecondParentId, subBranch);

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


		private string GetBranchName(MCommit mCommit)
		{
			if (!string.IsNullOrEmpty(mCommit.BranchName))
			{
				return mCommit.BranchName;
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


		private void SetMasterBranchCommits(IReadOnlyList<MSubBranch> branches, MModel xmodel)
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


		private void SetBranchNameWithPriority(MModel xmodel, string id, MSubBranch subBranch)
		{
			List<MCommit> pullmerges = new List<MCommit>();

			while (true)
			{
				if (id == null)
				{
					break;
				}

				MCommit mCommit = xmodel.Commits[id];

				if (mCommit.BranchName == subBranch.Name)
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

				mCommit.BranchName = subBranch.Name;
				mCommit.SubBranchId = subBranch.Id;


				id = mCommit.FirstParentId;
			}

			foreach (MCommit xCommit in pullmerges)
			{
				//SetBranchNameWithPriority(xmodel, xCommit.SecondParentId, subBranch);
				//RemovePullMergeBranch(xmodel, xBranch, xCommit.SecondParentId);
			}
		}


		private void SetChildren(IReadOnlyList<MCommit> commits)
		{
			foreach (MCommit xCommit in commits)
			{
				bool isFirstParent = true;
				foreach (MCommit parent in xCommit.Parents)
				{
					if (!parent.Children.Contains(xCommit))
					{
						parent.ChildIds.Add(xCommit.Id);
					}

					if (isFirstParent)
					{
						isFirstParent = false;
						if (!parent.FirstChildren.Contains(xCommit))
						{
							parent.FirstChildIds.Add(xCommit.Id);
						}
					}
				}
			}
		}


		private void SetSpecifiedCommitBranchNames(
			IReadOnlyList<CommitBranch> commitBranches,
			MModel xmodel)
		{
			foreach (CommitBranch commitBranch in commitBranches)
			{
				MCommit mCommit;
				if (xmodel.Commits.TryGetValue(commitBranch.CommitId, out mCommit))
				{
					mCommit.BranchNameSpecified = commitBranch.BranchName;
					mCommit.BranchName = commitBranch.BranchName;
					mCommit.SubBranchId = commitBranch.SubBranchId;
				}
			}
		}


		private void SetSubjectCommitBranchNames(
			IReadOnlyList<MCommit> commits,
			MModel xmodel)
		{
			foreach (MCommit xCommit in commits)
			{
				xCommit.BranchNameFromSubject = TryExtractBranchNameFromSubject(xCommit, xmodel);
			}
		}



		private IReadOnlyList<MCommit> AddCommits(IReadOnlyList<GitCommit> gitCommits, MModel xmodel)
		{
			return gitCommits.Select(
				c =>
				{
					MCommit mCommit = ToCommit(c, xmodel);
					xmodel.Commits.Add(mCommit);
					return mCommit;
				})
				.ToList();
		}


		private IReadOnlyList<MSubBranch> AddActiveBranches(
			IReadOnlyList<GitBranch> gitBranches, MModel xmodel)
		{
			return gitBranches.Select(gitBranch =>
			{
				MSubBranch subBranch = ToBranch(gitBranch, xmodel);
				xmodel.SubBranches.Add(subBranch);
				return subBranch;
			})
			.ToList();
		}


		private MSubBranch ToBranch(GitBranch gitBranch, MModel mModel)
		{
			string latestCommitId = gitBranch.LatestCommitId;
			
			return new MSubBranch(mModel)
			{
				Id = Guid.NewGuid().ToString(),
				Name = gitBranch.Name,			
				LatestCommitId = latestCommitId,
				IsMultiBranch = false,
				IsActive = true,
				IsRemote = gitBranch.IsRemote || gitBranch.LatestTrackingCommitId != null
			};
		}


		private MCommit ToCommit(GitCommit gitCommit, MModel mModel)
		{
			MergeBranchNames branchNames = ParseMergeNamesFromSubject(gitCommit);

			return new MCommit(mModel)
			{
				Id = gitCommit.Id,
				ShortId = gitCommit.ShortId,
				Subject = gitCommit.Subject,
				Author = gitCommit.Author,
				AuthorDate = gitCommit.DateTime.ToShortDateString() +
					"T" + gitCommit.DateTime.ToShortTimeString(),
				CommitDate = gitCommit.CommitDate.ToShortDateString() +
					 "T" + gitCommit.CommitDate.ToShortTimeString(),
				ParentIds = gitCommit.ParentIds.ToList(),
				MergeSourceBranchNameFromSubject = branchNames.SourceBranchName,
				MergeTargetBranchNameFromSubject = branchNames.TargetBranchName,
			};
		}


		private string TryExtractBranchNameFromSubject(MCommit mCommit, MModel mModel)
		{
			//if (xCommit.ShortId == "68e784")
			//{

			//}

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
				MCommit child = mModel.Commits[childId];
				if (child.SecondParentId == mCommit.Id
					&& !string.IsNullOrEmpty(child.MergeSourceBranchNameFromSubject))
				{
					return child.MergeSourceBranchNameFromSubject;
				}
			}

			return null;
		}

		private MergeBranchNames ParseMergeNamesFromSubject(GitCommit gitCommit)
		{
			if (gitCommit.ParentIds.Count <= 1)
			{
				// This is no merge commit, i.e. no branch names to parse
				return BranchNameParser.NoMerge;
			}

			return BranchNameParser.ParseBranchNamesFromSubject(gitCommit.Subject);
		}
	}


	internal class CommitBranch
	{
		public string CommitId { get; set; }
		public string BranchName { get; set; }
		public string SubBranchId { get; set; }


		public CommitBranch(string commitId, string branchName, string subBranchId)
		{
			CommitId = commitId;
			BranchName = branchName;
			SubBranchId = subBranchId;
		}
	}
}