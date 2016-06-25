using System.Collections.Generic;
using System.Linq;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class CommitsService : ICommitsService
	{
		public IReadOnlyList<MCommit> AddCommits(
			IReadOnlyList<GitCommit> gitCommits,
			IReadOnlyList<SpecifiedBranch> specifiedBranches,
			MRepository mRepository)
		{
			Timing t = new Timing();
			IReadOnlyList<MCommit> commits = AddCommits(gitCommits, mRepository);
			t.Log($"added {commits.Count}commits");

			SetChildren(commits);
			t.Log("Set children");

			SetSpecifiedCommitBranchNames(specifiedBranches, mRepository);
			t.Log("Set specified branch names");

			SetSubjectCommitBranchNames(commits, mRepository);
			t.Log("Parse subject branch names");
			return commits;
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



		private MCommit ToCommit(GitCommit gitCommit, MRepository mRepository)
		{
			MergeBranchNames branchNames = ParseMergeNamesFromSubject(gitCommit);

			return new MCommit
			{
				Repository = mRepository,
				Id = gitCommit.Id,
				ShortId = gitCommit.ShortId,
				Subject = GetSubjectWithoutTickets(gitCommit),
				Author = gitCommit.Author,
				AuthorDate = gitCommit.AuthorDate,
				CommitDate = gitCommit.CommitDate,
				Tickets = GetTickets(gitCommit),
				ParentIds = gitCommit.ParentIds.ToList(),
				MergeSourceBranchNameFromSubject = branchNames.SourceBranchName,
				MergeTargetBranchNameFromSubject = branchNames.TargetBranchName,
			};
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
			IReadOnlyList<MCommit> commits,
			MRepository xmodel)
		{
			foreach (MCommit xCommit in commits)
			{
				xCommit.BranchNameFromSubject = TryExtractBranchNameFromSubject(xCommit, xmodel);
			}
		}



		private IReadOnlyList<MCommit> AddCommits(
			IReadOnlyList<GitCommit> gitCommits, MRepository repository)
		{
			return gitCommits.Select(
				c =>
				{
					MCommit mCommit = ToCommit(c, repository);
					repository.Commits.Add(mCommit);
					return mCommit;
				})
				.ToList();
		}


		private string GetSubjectWithoutTickets(GitCommit commit)
		{
			string tickets = GetTickets(commit);
			return commit.Subject.Substring(tickets.Length);
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


		private MergeBranchNames ParseMergeNamesFromSubject(GitCommit gitCommit)
		{
			if (gitCommit.ParentIds.Count <= 1)
			{
				// This is no merge commit, i.e. no branch names to parse
				return BranchNameParser.NoMerge;
			}

			return BranchNameParser.ParseBranchNamesFromSubject(gitCommit.Subject);
		}



		private string GetTickets(GitCommit commit)
		{
			if (commit.Subject.StartsWith("#"))
			{
				int index = commit.Subject.IndexOf(" ");
				if (index > 1)
				{
					return commit.Subject.Substring(0, index) + " ";
				}
				if (index > 0)
				{
					index = commit.Subject.IndexOf(" ", index + 1);
					return commit.Subject.Substring(0, index) + " ";
				}
			}

			return "";
		}

	}
}
