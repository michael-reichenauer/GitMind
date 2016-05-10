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

			XModel xModel = new XModel();
			Timing t = new Timing();
			IReadOnlyList<XCommit> commits = AddCommits(gitCommits, xModel);
			t.Log("added commits");

			SetChildren(commits, xModel);
			t.Log("Set children");

			SetSpecifiedCommitBranchNames(commitBranches, xModel);
			t.Log("Set specified branch");

			SetSubjectCommitBranchNames(commits, xModel);
			t.Log("Set branch from subject");


			IReadOnlyList<XBranch> branches1 = AddActiveBranches(gitBranches, xModel);
			t.Log("Add branches");
			Log.Debug($"Number of active branches {branches1.Count} ({xModel.AllBranches.Count})");

			IReadOnlyList<XBranch> branches2 = AddMergedInactiveBranches(commits, xModel);
			IReadOnlyList<XBranch> branches = branches1.Concat(branches2).ToList();
			t.Log("Add merged branches");
			Log.Debug($"Number of inactive branches {branches2.Count} ({xModel.AllBranches.Count})");
			//branches2.ForEach(b => Log.Debug($"   Branch {b}"));

			SetMasterBranchCommits(branches, xModel);
			t.Log("Set master branch commits");
			Log.Debug($"Number of branches {xModel.AllBranches.Count}");

			SetBranchCommits(branches, xModel);
			t.Log("Set branch commits");
			Log.Debug($"Unset commits {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");

			//SetBranchCommitsOfChildern(commits, xmodel);
			//Log.Debug($"Unset commits {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");

			SetBranchCommitsOfSubjects(commits);
			t.Log("Set branch name from subjects");
			Log.Debug($"Unset commits {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");

			SetEmptyParentCommits(commits, xModel);
			t.Log("Set empty parent commits");
			Log.Debug($"Unset commits {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");

			//SetEmptyChildCommits(commits, xmodel);
			//t.Log("Set empty child commits");
			//Log.Debug($"Unset commits {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");

			SetBranchCommitsOfParents(commits, xModel);
			t.Log("Set branch names of parent");
			Log.Debug($"Unset commits {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");

			IReadOnlyList<XBranch> branches3 = AddMultiBranches(commits, xModel);
			t.Log("Add multi branches");
			Log.Debug($"Number of multi branches {branches3.Count} ({xModel.AllBranches.Count})");
			//branches3.ForEach(b => Log.Debug($"   Branch {b}"));

			SetBranchCommits(branches3, xModel);
		
			Log.Debug($"Unset commits after multi {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");
			Log.Debug($"All branches ({xModel.AllBranches.Count})");
			//xmodel.AllBranches.ForEach(b => Log.Debug($"   Branch {b}"));

			//List<XBranch> xBranches = xModel.AllBranches.Where(b => xModel.AllBranches.Any(b2 => b2 != b && b2.Name == b.Name)).ToList();

			//Log.Debug($"Duplicates count ({xBranches.Count})");
			//xBranches.OrderBy(b => b.Name).ForEach(b => Log.Debug($"  branches {b}"));


			ReduceDuplicateBranches(xModel.AllBranches, xModel);

			Log.Debug($"Number of branches {xModel.AllBranches.Count}");

			return xModel;
		}


		private void ReduceDuplicateBranches(IReadOnlyList<XBranch> allBranches, XModel xModel)
		{
			//throw new NotImplementedException();
		}


		public static IEnumerable<XCommit>FirstParents(XCommit xCommit, XModel xModel)
		{	
			while (!IsNone(xCommit.FirstParentId))
			{
				xCommit = xModel.Commit[xCommit.FirstParentId];
				yield return xCommit;
			}
		}


		public static IEnumerable<XCommit> SingleFirstChildren(XCommit xCommit, XModel xModel)
		{
			while (xCommit.FirstChildIds.Count == 1)
			{
				xCommit = xModel.Commit[xCommit.FirstChildIds[0]];
				yield return xCommit;
			}
		}

		private static bool IsNone(string id)
		{
			return string.IsNullOrEmpty(id);
		}


		private void SetEmptyParentCommits(IReadOnlyList<XCommit> commits, XModel xModel)
		{
			IEnumerable<XCommit> endCommits =
				commits.Where(c =>
				!string.IsNullOrEmpty(c.BranchName)
				&& !string.IsNullOrEmpty(c.FirstParentId)
				&& string.IsNullOrEmpty(xModel.Commit[c.FirstParentId].BranchName));

			foreach (XCommit xCommit in endCommits)
			{
				string branchName = xCommit.BranchName;

				XCommit last = xCommit;
				bool isFound = false;
				foreach (XCommit current in FirstParents(xCommit, xModel))
				{
					if ((!string.IsNullOrEmpty(current.BranchName) && current.BranchName != branchName))
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
					foreach (XCommit current in FirstParents(xCommit, xModel))
					{
						current.BranchName = branchName;

						if (current == last)
						{
							break;
						}
					}
				}
			}
		}

		//private void SetEmptyChildCommits(IReadOnlyList<XCommit> commits, XModel xModel)
		//{
		//	IEnumerable<XCommit> endCommits =
		//		commits.Where(c =>
		//		!string.IsNullOrEmpty(c.BranchName)
		//		&& c.FirstChildIds.Count == 1
		//		&& string.IsNullOrEmpty(xModel.Commit[c.FirstChildIds[0]].BranchName));

		//	foreach (XCommit xCommit in endCommits)
		//	{
		//		string branchName = xCommit.BranchName;

		//		XCommit last = xCommit;
		//		bool isFound = false;
		//		foreach (XCommit current in SingleFirstChildren(xCommit, xModel))
		//		{
		//			if ((!string.IsNullOrEmpty(current.BranchName) && current.BranchName != branchName))
		//			{
		//				// found commit with branch name already set 
		//				break;
		//			}

		//			string currentBranchName = GetBranchName(current);
		//			if (string.IsNullOrEmpty(currentBranchName) || currentBranchName == branchName)
		//			{
		//				last = current;
		//			}

		//			if (currentBranchName == branchName)
		//			{
		//				isFound = true;
		//			}
		//		}

		//		if (isFound || last.FirstChildIds.Count == 0)
		//		{
		//			foreach (XCommit current in SingleFirstChildren(xCommit, xModel))
		//			{
		//				current.BranchName = branchName;

		//				if (current == last)
		//				{
		//					break;
		//				}
		//			}
		//		}
		//	}
		//}


		private void SetBranchCommitsOfParents(IReadOnlyList<XCommit> commits, XModel xModel)
		{
			IEnumerable<XCommit> commitRoots =
				commits.Where(c =>
				!string.IsNullOrEmpty(c.BranchName)
				&& !string.IsNullOrEmpty(c.FirstParentId)
				&& string.IsNullOrEmpty(xModel.Commit[c.FirstParentId].BranchName));

			foreach (XCommit xCommit in commitRoots)
			{
				string branchName = xCommit.BranchName;

				foreach (XCommit current in FirstParents(xCommit, xModel))
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
				}
			}
		}

		private IReadOnlyList<XBranch> AddMultiBranches(IReadOnlyList<XCommit> commits, XModel xmodel)
		{
			IEnumerable<XCommit> roots =
				commits.Where(c =>
				string.IsNullOrEmpty(c.BranchName)
				&& c.FirstChildIds.Count > 1);

			Log.Debug($"Branch root count {roots.Count()}");

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
			IReadOnlyList<XCommit> commits, XModel xModel)
		{
			List<XBranch> branches = new List<XBranch>();

			// Commits which has no child, which has this commit as a first parent, i.e. it is the 
			// top of a branch and there is no existing branch at this commit
			IEnumerable<XCommit> topCommits = commits.Where(commit =>
				!commit.FirstChildIds.Any()
				&& !xModel.AllBranches.Any(
					b =>b.LastestLocalCommitId == commit.Id
					    || b.LastestTrackingCommitId == commit.Id));

			foreach (XCommit xCommit in topCommits)
			{	
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

				xModel.AllBranches.Add(xBranch);
				xModel.IdToBranch[xBranch.Id] = xBranch;
				branches.Add(xBranch);
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

				if (!(xBranch.IsMultiBranch && currentId == id))
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

				//RemovePullMergeBranch(xmodel, xBranch, xCommit.SecondParentId);
			}
		}


		private bool IsPullMergeCommit(XCommit xCommit, XBranch xBranch, XModel xmodel)
		{
			return
				xCommit.SecondParentId != null
				&& (xCommit.MergeSourceBranchNameFromSubject == xBranch.Name
					|| GetBranchName(xmodel.Commit[xCommit.SecondParentId]) == xBranch.Name);
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

				if (xCommit.ShortId == "6a3407")
				{
					
				}

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
				//RemovePullMergeBranch(xmodel, xBranch, xCommit.SecondParentId);
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