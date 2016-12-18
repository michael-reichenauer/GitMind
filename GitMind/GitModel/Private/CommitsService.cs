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
			IEnumerable<string> rootCommits = gitRepository.Branches.Select(b => b.TipId);

			if (gitRepository.Head.IsDetached)
			{
				rootCommits = rootCommits.Concat(new[] { gitRepository.Head.TipId });
			}

			rootCommits = rootCommits.ToList();
			t.Log("Root commit ids");


			Dictionary<string, object> added = new Dictionary<string, object>();

			Dictionary<CommitId, BranchName> branchNameByCommitId = new Dictionary<CommitId, BranchName>();
			Dictionary<CommitId, BranchName> subjectBranchNameByCommitId = new Dictionary<CommitId, BranchName>();

			Stack<string> commitShas = new Stack<string>();
			rootCommits.ForEach(sha => commitShas.Push(sha));
			rootCommits.ForEach(sha => added[sha] = null);
			t.Log("Pushed roots on stack");

			while (commitShas.Any())
			{
				//GitCommit gitCommit = commits.Pop();
				string commitSha = commitShas.Pop();
				CommitId commitId = new CommitId(commitSha);			

				if (!repository.GitCommits.TryGetValue(commitId, out GitCommit gitCommit))
				{
					// This git commit id has not yet been seen before
					gitCommit = gitRepository.GetCommit(commitSha);
					if (IsMergeCommit(gitCommit))
					{
						TrySetBranchNameFromSubject(commitId, gitCommit, branchNameByCommitId, subjectBranchNameByCommitId);
					}

					repository.GitCommits[commitId] = gitCommit;
				}

				if (!repository.Commits.TryGetValue(commitId, out MCommit commit))
				{
					commit = new MCommit()
					{
						Repository = repository,
						Id = commitId,
						ViewCommitId = commitId
					};

					repository.Commits[commitId] = commit;

					AddCommit(commit, gitCommit);

					AddParents(gitCommit.ParentIds, commitShas, added);			
				}

			

				BranchName branchName;
				if (branchNameByCommitId.TryGetValue(commitId, out branchName))
				{
					// Branch name set by a child commit (pull merge commit)
					commit.BranchName = branchName;
					gitCommit.SetBranchName(branchName);
				}

				BranchName subjectBranchName;
				if (subjectBranchNameByCommitId.TryGetValue(commitId, out subjectBranchName))
				{
					// Subject branch name set by a child commit (merge commit)
					commit.FromSubjectBranchName = subjectBranchName;
					gitCommit.SetBranchNameFromSubject(subjectBranchName);
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
			string subject = gitCommit.Subject;
			string tickets = GetTickets(subject);
			commit.Tickets = tickets;

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
			IEnumerable<CommitId> parents,
			Stack<GitCommit> commits,
			Dictionary<string, object> added)
		{
			parents.ForEach(parent =>
			{
				if (!added.ContainsKey(parent.Sha))
				{
					commits.Push(parent);
					added[parent.Sha] = null;
				}
			});
		}


		private static void TrySetBranchNameFromSubject(
			CommitId commitId,
			GitCommit gitCommit,
			IDictionary<CommitId, BranchName> branchNameByCommitId,
			IDictionary<CommitId, BranchName> subjectBranchNameByCommitId)
		{
			// Trying to parse source and target branch names from subject. They can be like 
			// "Merge branch 'branch-name' of remote-repository-path"
			// This is considered a "pull merge", where branch-name is both source and target. These are
			// usually automatically created by tools and thus more trustworthy.
			// Other merge merge subjects are less trustworthy since they sometiems are manually edited
			// like:
			// "Merge source-branch"
			// which contains a source branch name, but sometimes they contain a target like
			// "Merge source-branch into target-branch"
			MergeBranchNames mergeNames = BranchNameParser.ParseBranchNamesFromSubject(gitCommit.Subject);

			if (IsPullMergeCommit(mergeNames))
			{
				// Pull merge subjects (source branch same as target) (trust worthy, so use branch name
				branchNameByCommitId[commitId] = mergeNames.SourceBranchName;
				branchNameByCommitId[gitCommit.ParentIds[0]] = mergeNames.SourceBranchName;
				branchNameByCommitId[gitCommit.ParentIds[1]] = mergeNames.SourceBranchName;

				// But also note the barnch name from subjects
				subjectBranchNameByCommitId[commitId] = mergeNames.SourceBranchName;
				subjectBranchNameByCommitId[gitCommit.ParentIds[0]] = mergeNames.SourceBranchName;
				subjectBranchNameByCommitId[gitCommit.ParentIds[1]] = mergeNames.SourceBranchName;
			}
			else
			{
				// Normal merge subject (less trustworthy)
				if (mergeNames.TargetBranchName != null)
				{
					// There was a target branch name
					subjectBranchNameByCommitId[commitId] = mergeNames.TargetBranchName;
				}

				if (mergeNames.SourceBranchName != null)
				{
					// There was a source branch name
					subjectBranchNameByCommitId[gitCommit.ParentIds[1]] = mergeNames.SourceBranchName;
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

			commit.Tickets = tickets;
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


		private static bool IsMergeCommit(GitCommit gitCommit)
		{
			return gitCommit.ParentIds.Count > 1;
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
