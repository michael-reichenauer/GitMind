using System.Collections.Generic;
using System.Linq;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal class CommitsService : ICommitsService
	{
		public IReadOnlyList<MCommit> AddCommits(
			IReadOnlyList<GitCommit> gitCommits,
			MRepository repository)
		{
			return gitCommits.Select(
				gitCommit =>
				{
					MCommit commit;
					if (!repository.Commits.TryGetValue(gitCommit.Id, out commit))
					{
						commit = new MCommit();
						commit.Id = gitCommit.Id;
						commit.Repository = repository;
						repository.Commits.Add(commit);
					}

					if (commit.Subject == null)
					{
						CopyToCommit(gitCommit, commit);
						SetChildOfAllParents(commit, repository);
					}

					return commit;
				})
				.ToList();
		}


		private static void SetChildOfAllParents(MCommit commit, MRepository repository)
		{
			bool isFirstParent = true;
			foreach (string parentId in commit.ParentIds)
			{
				MCommit parent;
				if (!repository.Commits.TryGetValue(parentId, out parent))
				{
					parent = new MCommit();
					parent.Id = parentId;
					parent.Repository = repository;
					repository.Commits.Add(parent);
				}

				if (!parent.Children.Contains(commit))
				{
					parent.ChildIds.Add(commit.Id);
				}

				if (isFirstParent)
				{
					isFirstParent = false;
					if (!parent.FirstChildren.Contains(commit))
					{
						parent.FirstChildIds.Add(commit.Id);
					}
				}
			}
		}


		private void CopyToCommit(GitCommit gitCommit, MCommit commit)
		{
			string tickets = GetTickets(gitCommit);
			MergeBranchNames branchNames = ParseMergeNamesFromSubject(gitCommit);

			commit.ShortId = gitCommit.ShortId;
			commit.Subject = GetSubjectWithoutTickets(gitCommit, tickets);
			commit.Author = gitCommit.Author;
			commit.AuthorDate = gitCommit.AuthorDate;
			commit.CommitDate = gitCommit.CommitDate;
			commit.Tickets = tickets;
			commit.ParentIds = gitCommit.ParentIds.ToList();
			commit.MergeSourceBranchNameFromSubject = branchNames.SourceBranchName;
			commit.MergeTargetBranchNameFromSubject = branchNames.TargetBranchName;
		}


		private MergeBranchNames ParseMergeNamesFromSubject(GitCommit gitCommit)
		{
			if (gitCommit.ParentIds.Count <= 1)
			{
				// This is no merge commit, i.e. no branch names to parse
				return BranchNameParser.NoMerge;
			}

			MergeBranchNames names = BranchNameParser.ParseBranchNamesFromSubject(gitCommit.Subject);

			return names;
		}


		private string GetSubjectWithoutTickets(GitCommit commit, string tickets)
		{
			return commit.Subject.Substring(tickets.Length);
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
