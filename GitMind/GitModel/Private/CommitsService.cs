using System.Collections.Generic;
using System.Linq;



namespace GitMind.GitModel.Private
{
	internal class CommitsService : ICommitsService
	{
		public void AddBranchCommits(LibGit2Sharp.Repository repo, MRepository repository)
		{
			IEnumerable<LibGit2Sharp.Commit> rootCommits = repo.Branches.Select(b => b.Tip);
			Dictionary<string, object> added = new Dictionary<string, object>();	

			Dictionary<string, string> branchNameByCommitId = new Dictionary<string, string>();
			Dictionary<string, string> subjectBranchNameByCommitId = new Dictionary<string, string>();

			Stack<LibGit2Sharp.Commit> commits = new Stack<LibGit2Sharp.Commit>();
			rootCommits.ForEach(c => commits.Push(c));
			rootCommits.ForEach(c => added[c.Id.Sha] = null);

			while (commits.Any())
			{
				LibGit2Sharp.Commit gitCommit = commits.Pop();

				MCommit commit;
				string commitId = gitCommit.Id.Sha;
				if (!repository.Commits.TryGetValue(commitId, out commit))
				{
					commit = AddCommit(commitId, gitCommit, repository);

					if (IsMergeCommit(commit))
					{
						TrySetBranchNameFromSubject(commit, branchNameByCommitId, subjectBranchNameByCommitId);
					}

					AddParents(gitCommit.Parents, commits, added);
				}

				string branchName;
				if (branchNameByCommitId.TryGetValue(commit.Id, out branchName))
				{
					// Branch name set by a child commit (pull merge commit)
					commit.BranchName = branchName;
				}

				string subjectBranchName;
				if (subjectBranchNameByCommitId.TryGetValue(commit.Id, out subjectBranchName))
				{
					// Subject branch name set by a child commit (merge commit)
					commit.FromSubjectBranchName = subjectBranchName;
				}
			}
		}


		private MCommit AddCommit(string commitId, LibGit2Sharp.Commit gitCommit, MRepository repository)
		{
			MCommit commit = new MCommit();
			commit.Repository = repository;

			CopyToCommit(gitCommit, commit);
			SetChildOfParents(commit);
			repository.Commits[commitId] = commit;
			return commit;
		}


		private static void AddParents(
			IEnumerable<LibGit2Sharp.Commit> parents,
			Stack<LibGit2Sharp.Commit> commits,
			Dictionary<string, object> added)
		{
			parents.ForEach(parent =>
			{
				if (!added.ContainsKey(parent.Id.Sha))
				{
					commits.Push(parent);
					added[parent.Id.Sha] = null;
				}
			});
		}


		private static void TrySetBranchNameFromSubject(
			MCommit commit,
			IDictionary<string, string> branchNameByCommitId,
			IDictionary<string, string> subjectBranchNameByCommitId)
		{
			MergeBranchNames mergeNames = BranchNameParser.ParseBranchNamesFromSubject(commit.Subject);

			if (IsPullMergeCommit(mergeNames))
			{
				// Pull merge subjects (source branch same as target) are most likely automatically created
				// during a pull and thus more reliable. Lets set branch name on commit and second parent 
				commit.BranchName = mergeNames.TargetBranchName;
				branchNameByCommitId[commit.SecondParentId] = mergeNames.SourceBranchName;
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
					subjectBranchNameByCommitId[commit.SecondParentId] = mergeNames.SourceBranchName;
				}
			}
		}


		private static void SetChildOfParents(MCommit commit)
		{
			bool isFirstParent = true;
			foreach (string parentId in commit.ParentIds)
			{
				IList<string> childIds = commit.Repository.ChildIds(parentId);
				if (!childIds.Contains(commit.Id))
				{
					childIds.Add(commit.Id);
				}

				if (isFirstParent)
				{
					isFirstParent = false;
					IList<string> firstChildIds = commit.Repository.FirstChildIds(parentId);
					if (!firstChildIds.Contains(commit.Id))
					{
						firstChildIds.Add(commit.Id);
					}
				}
			}
		}


		private void CopyToCommit(LibGit2Sharp.Commit gitCommit, MCommit commit)
		{
			string subject = gitCommit.MessageShort;
			string tickets = GetTickets(subject);

			commit.Id = gitCommit.Id.Sha;
			commit.ShortId = gitCommit.Id.Sha.Substring(0, 6);
			commit.Subject = GetSubjectWithoutTickets(subject, tickets);
			commit.Author = gitCommit.Author.Name;
			commit.AuthorDate = gitCommit.Author.When.LocalDateTime;
			commit.CommitDate = gitCommit.Committer.When.LocalDateTime;
			commit.Tickets = tickets;
			commit.ParentIds = gitCommit.Parents.Select(c => c.Id.Sha).ToList();
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


		private static string GetSubjectWithoutTickets(string subject, string tickets)
		{
			return subject.Substring(tickets.Length);
		}


		private string GetTickets(string subject)
		{
			if (subject.StartsWith("#"))
			{
				int index = subject.IndexOf(" ");
				if (index > 1)
				{
					return subject.Substring(0, index) + " ";
				}
				if (index > 0)
				{
					index = subject.IndexOf(" ", index + 1);
					return subject.Substring(0, index) + " ";
				}
			}

			return "";
		}
	}
}
