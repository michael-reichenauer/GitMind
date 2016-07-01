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
			MRepository repository)
		{
			Timing t = new Timing();
			IReadOnlyList<MCommit> commits = AddGitCommits(gitCommits, repository);
			t.Log($"added {commits.Count} commits");

			SetChildren(commits);
			t.Log("Set children");

			return commits;
		}


		private IReadOnlyList<MCommit> AddGitCommits(
			IReadOnlyList<GitCommit> gitCommits, MRepository repository)
		{
			return gitCommits.Select(
				gitCommit =>
				{
					MCommit commit = ToCommit(gitCommit, repository);
					repository.Commits.Add(commit);
					return commit;
				})
				.ToList();
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
			string tickets = GetTickets(gitCommit);
			MergeBranchNames branchNames = ParseMergeNamesFromSubject(gitCommit);

			return new MCommit
			{
				Repository = mRepository,
				Id = gitCommit.Id,
				ShortId = gitCommit.ShortId,
				Subject = GetSubjectWithoutTickets(gitCommit, tickets),
				Author = gitCommit.Author,
				AuthorDate = gitCommit.AuthorDate,
				CommitDate = gitCommit.CommitDate,
				Tickets = tickets,
				ParentIds = gitCommit.ParentIds.ToList(),
				MergeSourceBranchNameFromSubject = branchNames.SourceBranchName,
				MergeTargetBranchNameFromSubject = branchNames.TargetBranchName,
			};
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
