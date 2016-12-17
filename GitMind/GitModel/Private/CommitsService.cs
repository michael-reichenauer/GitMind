using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Common;
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

			Dictionary<CommitId, BranchName> branchNameByCommitId = new Dictionary<CommitId, BranchName>();
			Dictionary<CommitId, BranchName> subjectBranchNameByCommitId = new Dictionary<CommitId, BranchName>();

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
				if (branchNameByCommitId.TryGetValue(commit.Id, out branchName))
				{
					// Branch name set by a child commit (pull merge commit)
					commit.BranchName = branchName;
				}

				BranchName subjectBranchName;
				if (subjectBranchNameByCommitId.TryGetValue(commit.Id, out subjectBranchName))
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
			MCommit commit = repository.Commit(CommitId.Uncommitted.Sha);
			repository.Uncommitted = commit;
			
			commit.IsVirtual = true;
			commit.BranchName = gitRepository.Head.Name;

			MCommit headCommit = repository.Commit(gitRepository.Head.TipId);
			CopyToUncommitedCommit(status, commit, headCommit.Id);
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
			IDictionary<CommitId, BranchName> branchNameByCommitId,
			IDictionary<CommitId, BranchName> subjectBranchNameByCommitId)
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
				IList<CommitId> childIds = parent.ChildIds;
				if (!childIds.Contains(commit.Id))
				{
					childIds.Add(commit.Id);
				}

				if (isFirstParent)
				{
					isFirstParent = false;
					IList<CommitId> firstChildIds = parent.FirstChildIds;
					if (!firstChildIds.Contains(commit.Id))
					{
						firstChildIds.Add(commit.Id);
					}
				}
			}
		}


		private void CopyToCommit(GitCommit gitCommit, MCommit commit)
		{
			string subject = gitCommit.Subject;
			string tickets = GetTickets(subject);

			commit.Subject = GetSubjectWithoutTickets(subject, tickets);
			commit.Author = gitCommit.Author;
			commit.AuthorDate = gitCommit.AuthorDate;
			commit.CommitDate = gitCommit.CommitDate;
			commit.Tickets = tickets;
			commit.ParentIds = gitCommit.Parents
				.Select(c => commit.Repository.Commit(c.Id).Id)
				.ToList();
		}

		private static void CopyToUncommitedCommit(Status status, MCommit commit, CommitId parentId)
		{
			int modifiedCount = status.ChangedCount;
			int conflictCount = status.ConflictCount;

			// commit.Id = MCommit.UncommittedId;
			//commit.CommitId = MCommit.UncommittedId;
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
			commit.ParentIds = new List<CommitId> { parentId };
			commit.BranchId = null;
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
