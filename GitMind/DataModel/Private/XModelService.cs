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

			SetCommitBranchNames(commitBranches, commits, xmodel);
			t.Log("Set commit branch names from subject");

			IReadOnlyList<XBranch> branches = AddActiveBranches(gitBranches, xmodel);
			t.Log("Add branches");

			IReadOnlyList<XBranch> branches2 = AddMergedInactiveBranches(commits, xmodel);
			branches = branches.Concat(branches2).ToList();
			t.Log("Add merged branches");	

			SetMasterBranchCommits(branches, xmodel);
			t.Log("Set master branch commits");

			SetBranchCommits(branches, xmodel);
			t.Log("Set branch commits");



			Log.Debug($"Unset commits {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");

			SetBranchCommitsOfChildern(commits, xmodel);

			Log.Debug($"Unset commits {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");

			SetBranchCommitsOfSubjects(commits);

			Log.Debug($"Unset commits {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");

			SetBranchCommitsOfChildern(commits, xmodel);

			Log.Debug($"Unset commits {commits.Count(c => string.IsNullOrEmpty(c.BranchName))}");

			Log.Debug($"Number of branches {xmodel.AllBranches.Count}");

			//var x = xmodel.AllBranches.Select(b => b.Name).ToList();
			//x.Sort();
			//x.ForEach(b => Log.Debug($"   Branch {b}"));
			return xmodel;
		}


		private void SetBranchCommitsOfChildern(IReadOnlyList<XCommit> commits, XModel xmodel)
		{
			IEnumerable<XCommit> commitsWithBranches =
				commits.Where(c => !string.IsNullOrEmpty(c.BranchName));

			foreach (XCommit xCommit in commitsWithBranches)
			{
				string branchName = xCommit.BranchName;

				XCommit current = xCommit;
				while (true)
				{
					if (string.IsNullOrEmpty(current.FirstParentId))
					{
						// Commit has no more parents
						break;
					}

					if (current.FirstChildIds.Count > 1)
					{
						// The parent has multiple children, which has the parent as first parent,
						// I.e. the parent is root of multiple branches 
						break;
					}

					XCommit firstParent = xmodel.Commit[current.FirstParentId];

					if (!string.IsNullOrEmpty(firstParent.BranchName))
					{
						// The child already has a branch name
						break;
					}

					firstParent.BranchName = branchName;
					current = firstParent;
				}


				current = xCommit;
				while (true)
				{					
					if (current.FirstChildIds.Count != 1)
					{
						// The parent has multiple children, which has the parent as first parent,
						// I.e. the parent is root of multiple branches 
						break;
					}

					XCommit firstChild = xmodel.Commit[current.FirstChildIds.ElementAt(0)];
					if (!string.IsNullOrEmpty(firstChild.BranchName))
					{
						// The child already has a branch name
						break;
					}

					firstChild.BranchName = branchName;
					current = firstChild;
				}
			}
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
			IReadOnlyList<XCommit> commits, XModel xmodel)
		{
			List<XBranch> branches = new List<XBranch>();

			int count = 0;
			foreach (XCommit xCommit in commits)
			{
				if (xCommit.ChildIds.Count > 0 && !xCommit.FirstChildIds.Any())
				{
					// The commit has children, but is not a first parent of any of the children
					count++;
					string branchName = GetBranchName(xCommit);
					if (string.IsNullOrEmpty(branchName))
					{
						branchName = "Branch_" + xCommit.Id;
					}

					// Log.Debug($"Merged Branch root {count} '{branchName}' has {xCommit.ChildIds.Count} children:");
					//xCommit.ChildIds.ForEach(id => Log.Debug($"   Merge child {xmodel.Commit[id]}"));

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
					// This commit has children, but no child has this commit has a first parent, i.e. this
					// commit is "top" of a branch. 

					//topCommits.Add(commit);
				}
				//else if (commit.Children.Count == 1 && commit.Children[0].SecondParent == commit)
				//{
				//	// The commit has one child, which is merge, where this commit is the source, 
				//	// i.e. it is a top (latest) commit in a branch merged into some other branch.
				//	// However it could also be a "pull merge commit" lets treat is as a candidate for now
				//	topCommits.Add(commit);
				//}
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
				if (b.Name != xBranch.Name && (b.LastestLocalCommitId == id || b.LastestTrackingCommitId == id))
				{
					XCommit c = xmodel.Commit[id];
					Log.Warn($"Commit {c} in branch {xBranch} same as other branch {b}");
					return;
				}
			}

			List<XCommit> pullmerges = new List<XCommit>();

			while (true)
			{
				if (id == null)
				{
					break;
				}

				XCommit xCommit = xmodel.Commit[id];

				if (!string.IsNullOrEmpty(xCommit.BranchName))
				{
					break;
				}

				if (IsPullMergeCommit(xCommit, xBranch, xmodel))
				{
					pullmerges.Add(xCommit);
				
				}

				if (xCommit.ChildIds.Count > 1)
				{
					if (0 != xCommit.ChildIds.Count(childId =>
						xmodel.Commit[childId].FirstParentId == id
						&& GetBranchName(xmodel.Commit[childId]) != xBranch.Name))
					{
						//Log.Warn($"Found commit which belongs to multiple different branches: {xCommit}");
						break;
					}
				}

				xCommit.BranchName = xBranch.Name;

				id = xCommit.FirstParentId;
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
			  && (xCommit.SourceBranchNameFromSubject == xBranch.Name
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


		private void SetCommitBranchNames(
			IReadOnlyList<CommitBranch> commitBranches,
			IReadOnlyList<XCommit> commits,
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
			MergeBranchNames branchNames = ParseBranchNamesFromSubject(gitCommit);

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
				SourceBranchNameFromSubject = branchNames.SourceBranchName,
				TargetBranchNameFromSubject = branchNames.TargetBranchName,
			};
		}


		private string TryExtractBranchNameFromSubject(XCommit xCommit, XModel xModel)
		{
			if (xCommit.SecondParentId != null)
			{
				// This is a merge commit, and the subject might contain the target (this current) branch 
				// name in the subject like e.g. "Merge <source-branch> into <target-branch>"
				string branchName = xCommit.TargetBranchNameFromSubject;
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
				if (child.SecondParentId == xCommit.Id && child.SourceBranchNameFromSubject != null)
				{
					return child.SourceBranchNameFromSubject;
				}
			}

			return null;
		}

		private MergeBranchNames ParseBranchNamesFromSubject(GitCommit gitCommit)
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