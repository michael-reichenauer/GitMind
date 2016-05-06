using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
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
			t.Log("Set Childern");

			IReadOnlyList<XBranch> branches = AddBranches(gitBranches, xmodel);
			t.Log("Add branches");

			SetMasterBranchCommits(branches, xmodel);
			t.Log("Set master branch commits");

			SetBranchCommits(branches, xmodel);
			t.Log("Set branch commits");

			SetCommitBranchNames(commitBranches, commits, xmodel);
			t.Log("Set commit branch names from subject");

			return xmodel;
		}


		private void SetBranchCommits(IReadOnlyList<XBranch> branches, XModel xmodel)
		{
			foreach (XBranch xBranch in branches.Where(b => b.Name != "master"))
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
				if (b != xBranch && (b.LastestLocalCommitId == id || b.LastestTrackingCommitId == id))
				{
					Log.Warn($"Id {id} in branch {xBranch} same as other branch {b}");
					return;
				}
			}

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

				if (xCommit.SecondParentId != null)
				{
					XCommit secondParent = xmodel.Commit[xCommit.SecondParentId];
					if (xCommit.SourceBranchNameFromSubject == xBranch.Name 
						|| GetBranchName(secondParent) == xBranch.Name)
					{
						SetBranchName(xmodel, xCommit.SecondParentId, xBranch);
					}
				}

				if (xCommit.ChildIds.Count > 1)
				{
					if (0 != xCommit.ChildIds.Count(childId =>
						xmodel.Commit[childId].FirstParentId == id
						&& GetBranchName(xmodel.Commit[childId]) != xBranch.Name))
					{
						Log.Warn($"Found commit which belongs to multiple different branches: {xCommit}");
						break;
					}
				}

				xCommit.BranchName = xBranch.Name;

				id = xCommit.FirstParentId;
			}
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

				if (xCommit.SecondParentId != null)
				{
					XCommit secondParent = xmodel.Commit[xCommit.SecondParentId];
					if (xCommit.SourceBranchNameFromSubject == xBranch.Name
						|| GetBranchName(secondParent) == xBranch.Name)
					{
						SetBranchNameWithPriority(xmodel, xCommit.SecondParentId, xBranch);
					}
				}

				xCommit.BranchName = xBranch.Name;

				id = xCommit.FirstParentId;
			}
		}


		private void SetChildren(IReadOnlyList<XCommit> commits, XModel xmodel)
		{
			foreach (XCommit xCommit in commits)
			{
				foreach (string parentId in xCommit.ParentIds)
				{
					XCommit parent = xmodel.Commit[parentId];
					if (!parent.ChildIds.Contains(xCommit.Id))
					{
						parent.ChildIds.Add(xCommit.Id);
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
					XCommit xCommit = ToXCommit(c);
					xmodel.AllCommits.Add(xCommit);
					xmodel.Commit[xCommit.Id] = xCommit;
					return xCommit;
				})
				.ToList();
		}


		private IReadOnlyList<XBranch> AddBranches(IReadOnlyList<GitBranch> gitBranches, XModel xmodel)
		{
			return gitBranches.Select(gitBranch =>
			{
				XBranch xBranch = ToXBranch(gitBranch);
				xmodel.AllBranches.Add(xBranch);
				xmodel.IdToBranch[xBranch.Id] = xBranch;
				return xBranch;
			})
			.ToList();
		}


		private XBranch ToXBranch(GitBranch gitBranch)
		{
			return new XBranch
			{
				Id = Guid.Empty.ToString(),
				Name = gitBranch.Name,
				TrackingName = gitBranch.TrackingBranchName,
				LastestLocalCommitId = gitBranch.IsRemote ? null : gitBranch.LatestCommitId,
				LastestTrackingCommitId = gitBranch.IsRemote
					? gitBranch.LatestCommitId
					: gitBranch.LatestTrackingCommitId,
				IsMultiBranch = false

			};
		}


		private XCommit ToXCommit(GitCommit gitCommit)
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