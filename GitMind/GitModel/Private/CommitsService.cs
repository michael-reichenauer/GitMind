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
						SetChildOfParents(commit, repository);

						if (IsMergeCommit(commit))
						{
							TrySetBranchNameFromSubject(commit);
						}
					}

					return commit;
				})
				.ToList();
		}


		private static void TrySetBranchNameFromSubject(MCommit commit)
		{
			MergeBranchNames mergeNames = BranchNameParser.ParseBranchNamesFromSubject(commit.Subject);

			if (IsPullMergeCommit(mergeNames))
			{
				// Pull merge subjects (source branch same as target) are most likely automatically created
				// during a pull and thus more reliable. Lets set branch name on commit and second parent 
				commit.BranchName = mergeNames.TargetBranchName;
				commit.SecondParent.BranchName = mergeNames.SourceBranchName;
			}
			else
			{
				// Often, merge subjects are automatically created, but sometimes manually edited and thus
				// not as trust worthy. Lets note the names, but hope for other more trust worthy sources.
				if (mergeNames.TargetBranchName != null)
				{
					commit.FromSubjectBranchName = mergeNames.TargetBranchName;
				}
				if (mergeNames.SourceBranchName != null)
				{
					commit.SecondParent.FromSubjectBranchName = mergeNames.SourceBranchName;
				}
			}
		}


		private static void SetChildOfParents(MCommit commit, MRepository repository)
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

			commit.ShortId = gitCommit.ShortId;
			commit.Subject = GetSubjectWithoutTickets(gitCommit, tickets);
			commit.Author = gitCommit.Author;
			commit.AuthorDate = gitCommit.AuthorDate;
			commit.CommitDate = gitCommit.CommitDate;
			commit.Tickets = tickets;
			commit.ParentIds = gitCommit.ParentIds.ToList();
		}


		private static bool IsMergeCommit(MCommit commit)
		{
			return commit.HasSecondParent;
		}


		private static bool IsPullMergeCommit(MergeBranchNames branchNames)
		{
			return
				branchNames.SourceBranchName != null
				&& branchNames.SourceBranchName == branchNames.TargetBranchName;
		}


		private static string GetSubjectWithoutTickets(GitCommit commit, string tickets)
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
