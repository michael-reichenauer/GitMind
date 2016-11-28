using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Features.StatusHandling;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class CommitsService : ICommitsService
	{
		public void AddBranchCommits(GitRepository gitRepository, MRepository repository)
		{
			Status status = repository.Status;

			Timing t = new Timing();
			IEnumerable<GitCommit> rootCommits = gitRepository.Branches.Select(b => b.Tip);

			if (gitRepository.Head.IsDetached)
			{
				rootCommits = rootCommits.Concat(new[] { gitRepository.Head.Tip });
			}

			rootCommits = rootCommits.ToList();
			t.Log("Root commit ids");


			Dictionary<string, object> added = new Dictionary<string, object>();

			Dictionary<int, BranchName> branchNameByCommitId = new Dictionary<int, BranchName>();
			Dictionary<int, BranchName> subjectBranchNameByCommitId = new Dictionary<int, BranchName>();

			Stack<GitCommit> commits = new Stack<GitCommit>();
			rootCommits.ForEach(c => commits.Push(c));
			rootCommits.ForEach(c => added[c.Id] = null);
			t.Log("Pushed roots on stack");

			while (commits.Any())
			{
				GitCommit gitCommit = commits.Pop();

				MCommit commit = repository.Commit(gitCommit.Id);
				if (commit.Subject == null)
				{
					AddCommit(commit, gitCommit);

					if (IsMergeCommit(commit))
					{
						TrySetBranchNameFromSubject(commit, branchNameByCommitId, subjectBranchNameByCommitId);
					}

					AddParents(gitCommit.Parents, commits, added);
				}

				BranchName branchName;
				if (branchNameByCommitId.TryGetValue(commit.IndexId, out branchName))
				{
					// Branch name set by a child commit (pull merge commit)
					commit.BranchName = branchName;
				}

				BranchName subjectBranchName;
				if (subjectBranchNameByCommitId.TryGetValue(commit.IndexId, out subjectBranchName))
				{
					// Subject branch name set by a child commit (merge commit)
					commit.FromSubjectBranchName = subjectBranchName;
				}
			}

			if (!status.IsOK)
			{
				// Adding a virtual "uncommitted" commit since current working folder status has some changes
				AddVirtualUncommitted(gitRepository, status, repository);
			}
		}


		private void AddCommit(MCommit commit, GitCommit gitCommit)
		{
			CopyToCommit(gitCommit, commit);
			SetChildOfParents(commit);
		}


		private void AddVirtualUncommitted(
			GitRepository gitRepository, Status status, MRepository repository)
		{
			MCommit commit = repository.Commit(MCommit.UncommittedId);
			repository.Uncommitted = commit;
			
			commit.IsVirtual = true;
			commit.BranchName = gitRepository.Head.Name;

			CopyToUncommitedCommit(status, commit, repository.Commit(gitRepository.Head.TipId).IndexId);
			commit.Author = gitRepository.UserName ?? "";

			SetChildOfParents(commit);
		}


		private static void AddParents(
			IEnumerable<GitCommit> parents,
			Stack<GitCommit> commits,
			Dictionary<string, object> added)
		{
			parents.ForEach(parent =>
			{
				if (!added.ContainsKey(parent.Id))
				{
					commits.Push(parent);
					added[parent.Id] = null;
				}
			});
		}


		private static void TrySetBranchNameFromSubject(
			MCommit commit,
			IDictionary<int, BranchName> branchNameByCommitId,
			IDictionary<int, BranchName> subjectBranchNameByCommitId)
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
			foreach (MCommit parent in commit.Parents)
			{
				IList<int> childIds = parent.ChildIds;
				if (!childIds.Contains(commit.IndexId))
				{
					childIds.Add(commit.IndexId);
				}

				if (isFirstParent)
				{
					isFirstParent = false;
					IList<int> firstChildIds = parent.FirstChildIds;
					if (!firstChildIds.Contains(commit.IndexId))
					{
						firstChildIds.Add(commit.IndexId);
					}
				}
			}
		}


		private void CopyToCommit(GitCommit gitCommit, MCommit commit)
		{
			string subject = gitCommit.Subject;
			string tickets = GetTickets(subject);

			commit.ShortId = gitCommit.ShortId;
			commit.Subject = GetSubjectWithoutTickets(subject, tickets);
			commit.Author = gitCommit.Author;
			commit.AuthorDate = gitCommit.AuthorDate;
			commit.CommitDate = gitCommit.CommitDate;
			commit.Tickets = tickets;
			commit.ParentIds = gitCommit.Parents
				.Select(c => commit.Repository.Commit(c.Id).IndexId)
				.ToList();
		}

		private static void CopyToUncommitedCommit(Status status, MCommit commit, int parentId)
		{
			int modifiedCount = status.ChangedCount;
			int conflictCount = status.ConflictCount;

			// commit.Id = MCommit.UncommittedId;
			//commit.CommitId = MCommit.UncommittedId;
			commit.ShortId = commit.CommitId.Substring(0, 6);
			commit.Subject = $"{modifiedCount} uncommitted changes in working folder";

			if (conflictCount > 0)
			{
				commit.Subject = 
					$"{conflictCount} conflicts and {modifiedCount} changes, {ShortSubject(status)}";
				commit.HasConflicts = true;
			}
			else if (status.IsMerging)
			{
				commit.Subject = $"{modifiedCount} changes, {ShortSubject(status)}";
				commit.IsMerging = true;
			}

			commit.Author = "";
			commit.AuthorDate = DateTime.Now;
			commit.CommitDate = DateTime.Now;
			commit.Tickets = "";
			commit.ParentIds = new List<int> { parentId };
		}


		private static string ShortSubject(Status status)
		{
			string subject = status.MergeMessage?.Trim() ?? "";
			string firstLine = subject.Split("\n".ToCharArray())[0];
			return firstLine;
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
