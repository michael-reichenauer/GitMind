using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.DataModel.Private
{
	internal class XModelService : IXModelService
	{
		public XModel XGetModel(IGitRepo gitRepo)
		{
			IReadOnlyList<GitCommit> gitCommits = gitRepo.GetAllCommts().ToList();
			IReadOnlyList<GitBranch> gitBranches = gitRepo.GetAllBranches();
			IReadOnlyList<CommitBranch> commitBranches = new CommitBranch[0];

			XModel xmodel = new XModel();
			Timing t = new Timing();
			IReadOnlyList<XCommit> commits = AddCommits(gitCommits, xmodel);
			t.Log("added commits");

			SetChildren(commits, xmodel);
			t.Log("Set children");

			SetSpecifiedCommitBranchNames(commitBranches, xmodel);
			t.Log("Set specified branch");

			SetSubjectCommitBranchNames(commits, xmodel);
			t.Log("Set branch from subject");


			IReadOnlyList<XBranch> branches = AddActiveBranches(gitBranches, xmodel);
			t.Log("Add branches");
			Log.Debug($"Number of branches {xmodel.AllBranches.Count}");

			IReadOnlyList<XBranch> branches2 = AddMergedInactiveBranches(commits, xmodel);
			branches = branches.Concat(branches2).ToList();
			t.Log("Add merged branches");
			Log.Debug($"Number of branches {xmodel.AllBranches.Count}");


			SetMasterBranchCommits(branches, xmodel);
			t.Log("Set master branch commits");
			Log.Debug($"Number of branches {xmodel.AllBranches.Count}");

			SetBranchCommits(branches, xmodel);
			t.Log("Set branch commits");
			Log.Debug($"Number of branches {xmodel.AllBranches.Count}");

			//commits
			//	.Where(c =>
			//		!string.IsNullOrEmpty(c.BranchName)
			//		&& !string.IsNullOrEmpty(c.BranchNameFromSubject)
			//		&& c.BranchName != c.BranchNameFromSubject
			//		&& !c.BranchNameFromSubject.Contains("trunk")
			//		&& !c.BranchNameFromSubject.Contains("Trunk"))
			//	.ForEach(c =>
			//		Log.Debug($"Commit has different branch names '{c.BranchName}'!='{c.BranchNameFromSubject}' {c}"));



			Log.Debug($"Unset commits {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");

			//SetBranchCommitsOfChildern(commits, xmodel);
			//Log.Debug($"Unset commits {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");

			SetBranchCommitsOfSubjects(commits);
			t.Log("Set branch name from subjects");

			Log.Debug($"Unset commits {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");

			SetEmptyCommits(commits, xmodel);
			t.Log("Set empty commits");
			Log.Debug($"Unset commits {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");

			SetBranchCommitsOfParents(commits, xmodel);
			t.Log("Set branch names of parent");
			Log.Debug($"Unset commits {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");

			IReadOnlyList<XBranch> branches3 = AddMultiBranches(commits, xmodel);
			t.Log("Add multi branches");
			Log.Debug($"Number of branches {xmodel.AllBranches.Count}");

			SetBranchCommits(branches3, xmodel);
			branches = branches.Concat(branches2).ToList();
			Log.Debug($"Unset commits after multi {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");


			//SetBranchCommitsOfChildern(commits, xmodel);
			//t.Log("Set branch names of childre");
			//Log.Debug($"Unset commits {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");


			//IEnumerable<XCommit> endCommits2 =
			//	commits.Where(c =>
			//	!string.IsNullOrEmpty(c.BranchName)
			//	&& !string.IsNullOrEmpty(c.FirstParentId)
			//	&& string.IsNullOrEmpty(xmodel.Commit[c.FirstParentId].BranchName));

			//Log.Debug($"Commits with empty first parents2 {endCommits2.Count()}");


			//IEnumerable<XCommit> endCommits3 =
			//	commits.Where(c =>
			//	!string.IsNullOrEmpty(c.BranchName)
			//	&& !string.IsNullOrEmpty(c.FirstParentId)
			//	&& string.IsNullOrEmpty(xmodel.Commit[c.FirstParentId].BranchName)
			//	&& xmodel.Commit[c.FirstParentId].FirstChildIds.Count < 2);

			//Log.Debug($"empty sss {endCommits3.Count()}");


			//foreach (XCommit xCommit in endCommits2)
			//{
			//	XCommit parent = xmodel.Commit[xCommit.FirstParentId];
			//	Log.Debug($"Commit {xCommit}");

			//}


			//SetBranchCommits2(branches, xmodel);
			//t.Log("Set branch commits");





			//Log.Debug($"Unset commits after Prio {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");

			//foreach (XCommit xCommit in commits.Where(c => string.IsNullOrEmpty(c.BranchName)))
			//{
			//	Log.Debug($"Commit {xCommit}");

			//	XCommit current = xCommit;
			//	while (true)
			//	{
			//		Log.Debug($"   Child Commit {current}");
			//		if (xCommit.FirstChildIds.Count < 1)
			//		{
			//			break;
			//		}

			//		current = xmodel.Commit[xCommit.FirstChildIds[0]];
			//	}
			//}

			Log.Debug($"Number of branches {xmodel.AllBranches.Count}");

			//var x = xmodel.AllBranches.Select(b => b.Name).ToList();
			//x.Sort();
			//x.ForEach(b => Log.Debug($"   Branch {b}"));
			return xmodel;
		}




		private void SetEmptyCommits(IReadOnlyList<XCommit> commits, XModel xmodel)
		{
			IEnumerable<XCommit> endCommits =
				commits.Where(c =>
				!string.IsNullOrEmpty(c.BranchName)
				&& !string.IsNullOrEmpty(c.FirstParentId)
				&& string.IsNullOrEmpty(xmodel.Commit[c.FirstParentId].BranchName));

			Log.Debug($"Commits with empty first parents {endCommits.Count()}");

			foreach (XCommit xCommit in endCommits)
			{
				string branchName = xCommit.BranchName;
				XCommit current = xmodel.Commit[xCommit.FirstParentId];
				XCommit last = current;
				bool isFound = false;
				while (true)
				{
					if ((!string.IsNullOrEmpty(current.BranchName) && current.BranchName != branchName))
					{
						// found some commit with branch name already set 
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

					if (string.IsNullOrEmpty(current.FirstParentId))
					{
						// found commit with no first parent (first commit on the branch)
						break;
					}

					current = xmodel.Commit[current.FirstParentId];
				}

				if (isFound)
				{
					// Did find a commit with 
					current = xmodel.Commit[xCommit.FirstParentId];
					while (true)
					{
						current.BranchName = branchName;
						if (current == last)
						{
							break;
						}

						current = xmodel.Commit[current.FirstParentId];
					}
				}
			}
		}



		private IReadOnlyList<XBranch> AddMultiBranches(IReadOnlyList<XCommit> commits, XModel xmodel)
		{
			//IEnumerable<XCommit> rootChildren =
			//	commits.Where(c =>
			//	!string.IsNullOrEmpty(c.BranchName)
			//	&& !string.IsNullOrEmpty(c.FirstParentId)
			//	&& string.IsNullOrEmpty(xmodel.Commit[c.FirstParentId].BranchName));

			IEnumerable<XCommit> roots =
				commits.Where(c =>
				string.IsNullOrEmpty(c.BranchName)
				&& c.FirstChildIds.Count > 1);

			Log.Debug($"Branch root count {roots.Count()}");
			//Log.Debug($"Possible Branch root count2 {rootChildren2.Count()}");

			//List<XCommit> roots = new List<XCommit>();
			//foreach (XCommit xCommit in rootChildren)
			//{
			//	XCommit parent = xmodel.Commit[xCommit.FirstParentId];

			//	if (!roots.Any(c => c.Id == parent.Id))
			//	{
			//		roots.Add(parent);
			//	}
			//}

			

			List<XBranch> branches = new List<XBranch>();
			Log.Debug($"Branch root count {roots.Count()}");
			foreach (XCommit root in roots)
			{
				string branchName = "Multibranch_" + root.ShortId;
				XBranch xBranch = new XBranch
				{
					Id = Guid.NewGuid().ToString(),
					Name = branchName,
					TrackingName = branchName,
					LastestLocalCommitId = null,
					LastestTrackingCommitId = root.Id,
					IsMultiBranch = true,
					IsActive = false
				};

				xmodel.AllBranches.Add(xBranch);
				xmodel.IdToBranch[xBranch.Id] = xBranch;
				branches.Add(xBranch);
			}

			return branches;
		}



		private void SetBranchCommitsOfParents(IReadOnlyList<XCommit> commits, XModel xmodel)
		{
			IEnumerable<XCommit> commitRoots =
				commits.Where(c =>
				!string.IsNullOrEmpty(c.BranchName)
				&& !string.IsNullOrEmpty(c.FirstParentId)
				&& string.IsNullOrEmpty(xmodel.Commit[c.FirstParentId].BranchName));


			foreach (XCommit xCommit in commitRoots)
			{
				string branchName = xCommit.BranchName;

				XCommit current = xmodel.Commit[xCommit.FirstParentId];
				while (true)
				{
					if (current.FirstChildIds.Count > 1)
					{
						// The commit has multiple children, which has the commit as first parent,
						// I.e. the commit is root of multiple branches 
						break;
					}

					if (!string.IsNullOrEmpty(current.BranchName))
					{
						// The commit already has a branch name
						break;
					}

					current.BranchName = branchName;

					if (string.IsNullOrEmpty(current.FirstParentId))
					{
						// Commit has no more parents
						break;
					}

					current = xmodel.Commit[current.FirstParentId];
				}
			}
		}


		//private void SetBranchCommitsOfChildern(IReadOnlyList<XCommit> commits, XModel xmodel)
		//{
		//	IEnumerable<XCommit> commitsWithBranches =
		//		commits.Where(c => !string.IsNullOrEmpty(c.BranchName));

		//	foreach (XCommit xCommit in commitsWithBranches)
		//	{
		//		string branchName = xCommit.BranchName;

		//		XCommit current = xCommit;
		//		while (true)
		//		{
		//			if (current.FirstChildIds.Count != 1)
		//			{
		//				// The parent has multiple children, which has the parent as first parent,
		//				// I.e. the parent is root of multiple branches 
		//				break;
		//			}

		//			XCommit firstChild = xmodel.Commit[current.FirstChildIds.ElementAt(0)];
		//			if (!string.IsNullOrEmpty(firstChild.BranchName))
		//			{
		//				// The child already has a branch name
		//				break;
		//			}

		//			firstChild.BranchName = branchName;
		//			current = firstChild;
		//		}
		//	}
		//}


		private void SetBranchCommitsOfSubjects(IReadOnlyList<XCommit> commits)
		{
			IEnumerable<XCommit> commitsWithoutBranches =
				commits.Where(c => string.IsNullOrEmpty(c.BranchName));

			foreach (XCommit xCommit in commitsWithoutBranches)
			{
				xCommit.BranchName = GetBranchName(xCommit);
			}
		}



		private IReadOnlyList<XBranch> AddMergedInactiveBranches(
			IReadOnlyList<XCommit> commits, XModel xmodel)
		{
			List<XBranch> branches = new List<XBranch>();

			foreach (XCommit xCommit in commits)
			{
				if (!xCommit.FirstChildIds.Any()
					&& !xmodel.AllBranches.Any(b => 
						b.LastestLocalCommitId == xCommit.Id
						|| b.LastestTrackingCommitId == xCommit.Id))
				{
					// The commit has no child, which has this commit as a first parent, i.e. it is the 
					// top of a branch and there is no existing branch at this commit
					string branchName = GetBranchName(xCommit);
					if (string.IsNullOrEmpty(branchName))
					{
						branchName = "Branch_" + xCommit.ShortId;
					}

					XBranch xBranch = new XBranch
					{
						Id = Guid.NewGuid().ToString(),
						Name = branchName,
						TrackingName = branchName,
						LastestLocalCommitId = null,
						LastestTrackingCommitId = xCommit.Id,
						IsMultiBranch = false,
						IsActive = false
					};

					xmodel.AllBranches.Add(xBranch);
					xmodel.IdToBranch[xBranch.Id] = xBranch;
					branches.Add(xBranch);
				}			
			}

			return branches;
		}


		private void SetBranchCommits(IReadOnlyList<XBranch> branches, XModel xmodel)
		{
			foreach (XBranch xBranch in branches.Where(b => b.Name != "master").ToList())
			{
				string id = xBranch.LastestLocalCommitId;
				SetBranchName(xmodel, id, xBranch);

				id = xBranch.LastestTrackingCommitId;
				SetBranchName(xmodel, id, xBranch);
			}
		}


		private void SetBranchName(XModel xmodel, string id, XBranch xBranch)
		{
			if (string.IsNullOrEmpty(id))
			{
				return;
			}

			foreach (XBranch b in xmodel.AllBranches)
			{
				if (b.Name != xBranch.Name
					&& !(xBranch.IsActive && !b.IsActive)
					&& (b.LastestLocalCommitId == id || b.LastestTrackingCommitId == id))
				{
					XCommit c = xmodel.Commit[id];
					Log.Warn($"Commit {c} in branch {xBranch} same as other branch {b}");
					return;
				}
			}

			List<XCommit> pullmerges = new List<XCommit>();

			string currentId = id;
			while (true)
			{
				if (currentId == null)
				{
					break;
				}

				XCommit xCommit = xmodel.Commit[currentId];

				if (!string.IsNullOrEmpty(xCommit.BranchName))
				{
					break;
				}

				if (IsPullMergeCommit(xCommit, xBranch, xmodel))
				{
					pullmerges.Add(xCommit);

				}

				if (!(xBranch.Name.StartsWith("Multibranch_") && currentId == id))
				{
					// for multi branches, first commit is a branch root
					if (xCommit.ChildIds.Count > 1)
					{
						if (0 != xCommit.ChildIds.Count(childId =>
							xmodel.Commit[childId].FirstParentId == currentId
							&& GetBranchName(xmodel.Commit[childId]) != xBranch.Name))
						{
							//Log.Warn($"Found commit which belongs to multiple different branches: {xCommit}");
							break;
						}
					}
				}

				xCommit.BranchName = xBranch.Name;

				currentId = xCommit.FirstParentId;
			}

			foreach (XCommit xCommit in pullmerges)
			{
				SetBranchName(xmodel, xCommit.SecondParentId, xBranch);

				RemovePullMergeBranch(xmodel, xBranch, xCommit);
			}
		}


		private static void RemovePullMergeBranch(XModel xmodel, XBranch xBranch, XCommit xCommit)
		{
			XBranch xSubBranch = xmodel.AllBranches.FirstOrDefault(
				b => b != xBranch
						 && (b.LastestLocalCommitId == xCommit.SecondParentId
								 || b.LastestTrackingCommitId == xCommit.SecondParentId));

			if (xSubBranch != null)
			{
				//Log.Warn($"Removing pull merge branch {xSubBranch}");
				xmodel.AllBranches.Remove(xSubBranch);
			}
		}


		private bool IsPullMergeCommit(XCommit xCommit, XBranch xBranch, XModel xmodel)
		{
			bool isPullMergeCommit =
				xCommit.SecondParentId != null
				&& (xCommit.MergeSourceBranchNameFromSubject == xBranch.Name
					|| GetBranchName(xmodel.Commit[xCommit.SecondParentId]) == xBranch.Name);

			//if (isPullMergeCommit)
			//{
			//	Log.Debug($"Pull merge commit {xCommit}");
			//}

			return isPullMergeCommit;
		}


		private string GetBranchName(XCommit xCommit)
		{
			if (!string.IsNullOrEmpty(xCommit.BranchName))
			{
				return xCommit.BranchName;
			}
			else if (!string.IsNullOrEmpty(xCommit.BranchNameSpecified))
			{
				return xCommit.BranchNameSpecified;
			}
			else if (!string.IsNullOrEmpty(xCommit.BranchNameFromSubject))
			{
				return xCommit.BranchNameFromSubject;
			}

			return null;
		}


		private void SetMasterBranchCommits(IReadOnlyList<XBranch> branches, XModel xmodel)
		{
			XBranch master = branches.FirstOrDefault(b => b.Name == "master");
			if (master != null)
			{
				string id = master.LastestLocalCommitId;

				SetBranchNameWithPriority(xmodel, id, master);
				id = master.LastestTrackingCommitId;
				SetBranchNameWithPriority(xmodel, id, master);
			}
		}


		private void SetBranchNameWithPriority(XModel xmodel, string id, XBranch xBranch)
		{
			List<XCommit> pullmerges = new List<XCommit>();

			while (true)
			{
				if (id == null)
				{
					break;
				}

				XCommit xCommit = xmodel.Commit[id];

				if (xCommit.BranchName == xBranch.Name)
				{
					break;
				}

				if (IsPullMergeCommit(xCommit, xBranch, xmodel))
				{
					pullmerges.Add(xCommit);

				}

				if (!string.IsNullOrEmpty(xCommit.BranchNameFromSubject) &&
					xCommit.BranchNameFromSubject != xBranch.Name)
				{
					//Log.Debug($"Setting different name '{xBranch.Name}'!='{xCommit.BranchNameFromSubject}'");
				}

				xCommit.BranchName = xBranch.Name;


				id = xCommit.FirstParentId;
			}

			foreach (XCommit xCommit in pullmerges)
			{
				SetBranchNameWithPriority(xmodel, xCommit.SecondParentId, xBranch);
				RemovePullMergeBranch(xmodel, xBranch, xCommit);
			}
		}


		private void SetChildren(IReadOnlyList<XCommit> commits, XModel xmodel)
		{
			foreach (XCommit xCommit in commits)
			{
				bool isFirstParent = true;
				foreach (string parentId in xCommit.ParentIds)
				{
					XCommit parent = xmodel.Commit[parentId];
					if (!parent.ChildIds.Contains(xCommit.Id))
					{
						parent.ChildIds.Add(xCommit.Id);
					}

					if (isFirstParent)
					{
						isFirstParent = false;
						if (!parent.FirstChildIds.Contains(xCommit.Id))
						{
							parent.FirstChildIds.Add(xCommit.Id);
						}
					}
				}
			}
		}


		private void SetSpecifiedCommitBranchNames(
			IReadOnlyList<CommitBranch> commitBranches,
			XModel xmodel)
		{
			foreach (CommitBranch commitBranch in commitBranches)
			{
				XCommit xCommit;
				if (xmodel.Commit.TryGetValue(commitBranch.CommitId, out xCommit))
				{
					xCommit.BranchNameSpecified = commitBranch.BranchName;
				}
			}
		}


		private void SetSubjectCommitBranchNames(
			IReadOnlyList<XCommit> commits,
			XModel xmodel)
		{
			foreach (XCommit xCommit in commits)
			{
				xCommit.BranchNameFromSubject = TryExtractBranchNameFromSubject(xCommit, xmodel);
			}
		}



		private IReadOnlyList<XCommit> AddCommits(IReadOnlyList<GitCommit> gitCommits, XModel xmodel)
		{
			return gitCommits.Select(
				c =>
				{
					XCommit xCommit = ToCommit(c);
					xmodel.AllCommits.Add(xCommit);
					xmodel.Commit[xCommit.Id] = xCommit;
					return xCommit;
				})
				.ToList();
		}


		private IReadOnlyList<XBranch> AddActiveBranches(
			IReadOnlyList<GitBranch> gitBranches, XModel xmodel)
		{
			return gitBranches.Select(gitBranch =>
			{
				XBranch xBranch = ToBranch(gitBranch);
				xmodel.AllBranches.Add(xBranch);
				xmodel.IdToBranch[xBranch.Id] = xBranch;
				return xBranch;
			})
			.ToList();
		}


		private XBranch ToBranch(GitBranch gitBranch)
		{
			return new XBranch
			{
				Id = Guid.NewGuid().ToString(),
				Name = gitBranch.Name,
				TrackingName = gitBranch.TrackingBranchName,
				LastestLocalCommitId = gitBranch.IsRemote ? null : gitBranch.LatestCommitId,
				LastestTrackingCommitId = gitBranch.IsRemote
					? gitBranch.LatestCommitId
					: gitBranch.LatestTrackingCommitId,
				IsMultiBranch = false,
				IsActive = true
			};
		}


		private XCommit ToCommit(GitCommit gitCommit)
		{
			MergeBranchNames branchNames = ParseMergeNamesFromSubject(gitCommit);

			return new XCommit
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


		private string TryExtractBranchNameFromSubject(XCommit xCommit, XModel xModel)
		{
			//if (xCommit.ShortId == "68e784")
			//{

			//}

			if (xCommit.SecondParentId != null)
			{
				// This is a merge commit, and the subject might contain the target (this current) branch 
				// name in the subject like e.g. "Merge <source-branch> into <target-branch>"
				string branchName = xCommit.MergeTargetBranchNameFromSubject;
				if (branchName != null)
				{
					return branchName;
				}
			}

			// If a child of this commit is a merge commit merged from this commit, lets try to get
			// the source branch name of that commit. I.e. a child commit might have a subject like
			// e.g. "Merge <source-branch> ..." That source branch would thus be the name of the branch
			// of this commit.
			foreach (string childId in xCommit.ChildIds)
			{
				XCommit child = xModel.Commit[childId];
				if (child.SecondParentId == xCommit.Id
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

		public CommitBranch(string commitId, string branchName)
		{
			CommitId = commitId;
			BranchName = branchName;
		}
	}
}