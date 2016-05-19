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

			SetChildren(commits);
			t.Log("Set children");

			SetSpecifiedCommitBranchNames(commitBranches, xModel);
			t.Log("Set specified branch names");

			SetSubjectCommitBranchNames(commits, xModel);
			t.Log("Parse subject branch names");

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
			Log.Debug($"Unset commits {commits.Count(c => !c.HasBranchName)}");
			Log.Debug($"Number of branches {xModel.AllBranches.Count}");

			SetBranchCommits(branches, xModel);
			t.Log("Set branch commits");
			Log.Debug($"Unset commits {commits.Count(c => !c.HasBranchName)}");
		
			SetEmptyParentCommits(commits);
			t.Log("Set empty parent commits");
			Log.Debug($"Unset commits {commits.Count(c => !c.HasBranchName)}");

			SetBranchCommitsOfParents(commits);
			t.Log("Set same branch name as parent with name");
			Log.Debug($"Unset commits {commits.Count(c => !c.HasBranchName)}");

			IReadOnlyList<XBranch> branches3 = AddMultiBranches(commits, branches, xModel);
			t.Log("Add multi branches");
			Log.Debug($"Number of multi branches {branches3.Count} ({xModel.AllBranches.Count})");
			SetBranchCommits(branches3, xModel);

			branches = branches.Concat(branches3).ToList();

			Log.Debug($"Unset commits after multi {commits.Count(c => !c.HasBranchName)}");
			commits.Where(c => string.IsNullOrEmpty(c.BranchName))
				.ForEach(c => Log.Warn($"   Unset {c} -> parent: {c.FirstParentId}"));
			Log.Debug($"All branches ({branches.Count})");

			FindBranchId(branches, xModel);

			Log.Debug($"Number of total branches {xModel.AllBranches.Count}");
			Log.Debug($"Number of total commits {xModel.AllCommits.Count}");
			Log.Debug($"Number of IsAnonymous branches {xModel.AllBranches.Count(b => b.IsAnonymous && !b.IsMultiBranch)}");
			return xModel;
		}


		private void FindBranchId(IReadOnlyList<XBranch> branches, XModel xModel)
		{
			foreach (XBranch xBranch in branches)
			{
				XCommit LatestCommit = xBranch.LatestCommit;

				IEnumerable<XCommit> commits = xBranch.LatestCommit.FirstAncestors()
					.TakeWhile(c => c.BranchName == xBranch.Name);

				if (commits.Any())
				{
					XCommit firstCommit = commits.Last();
					xBranch.FirstCommitId = firstCommit.Id;
					xBranch.ParentCommitId = firstCommit.FirstParentId;
				}
				else
				{
					if (LatestCommit.BranchName != null)
					{
						xBranch.FirstCommitId = LatestCommit.Id;
						xBranch.ParentCommitId = LatestCommit.FirstParentId;
					}
					else
					{
						Log.Warn($"Branch with no commits {xBranch}");
					}
				}
			}

		
			int branchCount = 0;
			var groupedOnName = branches.GroupBy(b => b.Name);
			int branchNameCount = groupedOnName.Count();

			foreach (var nameGroup in groupedOnName)
			{
				var groupedOnParentCommitId = nameGroup.GroupBy(b => b.ParentCommitId);


				foreach (var branchGroup in groupedOnParentCommitId)
				{
					XBranch xBranch = branchGroup.First();
					XBranchCommits xBranchCommits = new XBranchCommits(xModel)
					{
						Name = xBranch.Name,
						Id = Guid.NewGuid().ToString(),

						IsMultiBranch = xBranch.IsMultiBranch,
						IsActive = xBranch.IsActive,
						IsAnonymous = xBranch.IsAnonymous,
						
						ParentCommitId = xBranch.ParentCommitId
					};

					xBranchCommits.Branches.AddRange(branchGroup);

					xBranchCommits.Commits.AddRange(
						branchGroup
							.SelectMany(branch =>
								branch.LatestCommit.FirstAncestors()
									.TakeWhile(c => c.Id != branch.ParentCommitId))
							.Distinct()
							.OrderBy(c => c.CommitDate));

				}



				// Log.Debug($"Name {group.Key} count: {groupedOnParentCommitId.Count()}");

					//if (groupedOnParentCommitId.Count() > 1)
					//{
					//	// Log.Debug($"Name {group.Key} count: {groupedOnParentCommitId.Count()}");
					//	foreach (var branchGroup in groupedOnParentCommitId)
					//	{
					//		if (branchGroup.Key != null)
					//		{
					//			XCommit xCommit = xModel.Commit[branchGroup.Key];
					//			//Log.Debug($"   RootId: {xCommit}");
					//		}
					//		else
					//		{
					//			// This happens if the branch has not parent "master" or a branch,
					//			// which has not even been branched from master
					//			Log.Warn($"   key is null");
					//		}
					//	}
					//}
			}

			Log.Debug($"Branch count all: {branches.Count}, actual {branchCount}, namecount {branchNameCount}");	
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


		private void SetEmptyParentCommits(IReadOnlyList<XCommit> commits)
		{
			// All commits, which do have a name, but first parent commit does not have a name
			IEnumerable<XCommit> commitsWithBranchName =
				commits.Where(commit =>
					commit.HasBranchName 
					&& commit.HasFirstParent 
					&& !commit.FirstParent.HasBranchName);

			foreach (XCommit xCommit in commitsWithBranchName)
			{
				string branchName = xCommit.BranchName;

				XCommit last = xCommit;
				bool isFound = false;
				foreach (XCommit current in xCommit.FirstAncestors())
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
					foreach (XCommit current in xCommit.FirstAncestors())
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


		private static void SetBranchCommitsOfParents(IReadOnlyList<XCommit> commits)
		{
			IEnumerable<XCommit> commitsWithBranchName =
				commits.Where(commit =>
					commit.HasBranchName
					&& commit.HasFirstParent
					&& !commit.FirstParent.HasBranchName);

			foreach (XCommit xCommit in commitsWithBranchName)
			{
				string branchName = xCommit.BranchName;

				foreach (XCommit current in xCommit.FirstAncestors()
					.TakeWhile(c => c.FirstChildIds.Count <= 1 && !c.HasBranchName))
				{
					current.BranchName = branchName;
				}
			}
		}


		private IReadOnlyList<XBranch> AddMultiBranches(
			IReadOnlyList<XCommit> commits, IReadOnlyList<XBranch> branches, XModel xmodel)
		{
			IEnumerable<XCommit> roots =
				commits.Where(c =>
				string.IsNullOrEmpty(c.BranchName)
				&& c.FirstChildIds.Count > 1);

			// The commits where multiple branches are starting and the commits has no branch name
			IEnumerable<XCommit> roots2 = branches
				.GroupBy(b => b.LatestCommitId)
				.Where(group => group.Count() > 1)
				.Select(group => xmodel.Commit[group.Key])
				.Where(c => string.IsNullOrEmpty(c.BranchName));

			roots = roots.Concat(roots2);
			Log.Debug($"Branch root count {roots.Count()}");

			List<XBranch> multiBranches = new List<XBranch>();
			foreach (XCommit root in roots)
			{
				string branchName = "Multibranch_" + root.ShortId;

				XBranch xBranch = new XBranch(xmodel)
				{
					Id = Guid.NewGuid().ToString(),
					Name = branchName,		
					LatestCommitId = root.Id,
					IsMultiBranch = true,
					IsActive = false,
					IsAnonymous = true
				};

				xmodel.AllBranches.Add(xBranch);
				xmodel.IdToBranch[xBranch.Id] = xBranch;
				multiBranches.Add(xBranch);
			}

			return multiBranches;
		}


		private IReadOnlyList<XBranch> AddMergedInactiveBranches(
			IReadOnlyList<XCommit> commits, XModel xModel)
		{
			List<XBranch> branches = new List<XBranch>();

			// Commits which has no child, which has this commit as a first parent, i.e. it is the 
			// top of a branch and there is no existing branch at this commit
			IEnumerable<XCommit> topCommits = commits.Where(commit =>
				!commit.FirstChildIds.Any()
				&& !xModel.AllBranches.Any(b =>b.LatestCommitId == commit.Id));
	
			foreach (XCommit xCommit in topCommits)
			{	
				XBranch xBranch = new XBranch(xModel)
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
					xBranch.IsAnonymous = true;
				}

				xBranch.Name = branchName;

				xModel.AllBranches.Add(xBranch);
				xModel.IdToBranch[xBranch.Id] = xBranch;
				branches.Add(xBranch);
			}


			return branches;
		}


		private string TryFindBranchName(XCommit xCommit)
		{
			string branchName = GetBranchName(xCommit);

			if (branchName == null)
			{
				int count = 0;
				// Could not find a branch name from the commit, lets try it ancestors
				foreach (XCommit commit in xCommit.FirstAncestors()
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


		private void SetBranchCommits(IReadOnlyList<XBranch> branches, XModel xmodel)
		{
			foreach (XBranch xBranch in branches.Where(b => b.Name != "master").ToList())
			{
				string id = xBranch.LatestCommitId;
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
					&& !(xBranch.IsMultiBranch)
					&& ( b.LatestCommitId == id))
				{
					XCommit c = xmodel.Commit[id];
					//Log.Warn($"Commit {c} in branch {xBranch} same as other branch {b}");
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
			// Local master
			XBranch master = branches.FirstOrDefault(b => b.Name == "master" && !b.IsRemote);
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
				//RemovePullMergeBranch(xmodel, xBranch, xCommit.SecondParentId);
			}
		}


		private void SetChildren(IReadOnlyList<XCommit> commits)
		{
			foreach (XCommit xCommit in commits)
			{
				bool isFirstParent = true;
				foreach (XCommit parent in xCommit.Parents)
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
			XModel xmodel)
		{
			foreach (CommitBranch commitBranch in commitBranches)
			{
				XCommit xCommit;
				if (xmodel.Commit.TryGetValue(commitBranch.CommitId, out xCommit))
				{
					xCommit.BranchNameSpecified = commitBranch.BranchName;
					xCommit.BranchName = commitBranch.BranchName;
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
					XCommit xCommit = ToCommit(c, xmodel);
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
				XBranch xBranch = ToBranch(gitBranch, xmodel);
				xmodel.AllBranches.Add(xBranch);
				xmodel.IdToBranch[xBranch.Id] = xBranch;
				return xBranch;
			})
			.ToList();
		}


		private XBranch ToBranch(GitBranch gitBranch, XModel xModel)
		{
			string latestCommitId = gitBranch.LatestCommitId;
			
			return new XBranch(xModel)
			{
				Id = Guid.NewGuid().ToString(),
				Name = gitBranch.Name,			
				LatestCommitId = latestCommitId,
				IsMultiBranch = false,
				IsActive = true,
				IsRemote = gitBranch.IsRemote || gitBranch.LatestTrackingCommitId != null
			};
		}


		private XCommit ToCommit(GitCommit gitCommit, XModel xModel)
		{
			MergeBranchNames branchNames = ParseMergeNamesFromSubject(gitCommit);

			return new XCommit(xModel)
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