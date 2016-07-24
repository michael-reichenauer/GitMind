using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal class CommitsService : ICommitsService
	{
		public void AddBranchCommits(
			GitRepository gitRepository, GitStatus gitStatus, MRepository repository)
		{
			IEnumerable<GitCommit> rootCommits = gitRepository.Branches.Select(b => b.Tip);
			Dictionary<string, object> added = new Dictionary<string, object>();

			Dictionary<string, string> branchNameByCommitId = new Dictionary<string, string>();
			Dictionary<string, string> subjectBranchNameByCommitId = new Dictionary<string, string>();

			Stack<GitCommit> commits = new Stack<GitCommit>();
			rootCommits.ForEach(c => commits.Push(c));
			rootCommits.ForEach(c => added[c.Id] = null);

			while (commits.Any())
			{
				GitCommit gitCommit = commits.Pop();

				MCommit commit;
				string commitId = gitCommit.Id;
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

			if (!gitStatus.OK)
			{
				// Adding a virtual "uncommitted" commit since current working folder status has some changes
				AddVirtualUncommitted(gitRepository, gitStatus, repository);
			}
		}


		private MCommit AddCommit(string commitId, GitCommit gitCommit, MRepository repository)
		{
			MCommit commit = new MCommit();
			commit.Repository = repository;

			CopyToCommit(gitCommit, commit);
			SetChildOfParents(commit);
			repository.Commits[commitId] = commit;
			return commit;
		}


		private void AddVirtualUncommitted(
			GitRepository gitRepository, GitStatus gitStatus, MRepository repository)
		{
			MCommit commit = new MCommit();
			commit.IsVirtual = true;
			commit.Repository = repository;
			commit.BranchName = gitRepository.Head.Name;

			CopyToCommit(gitStatus, commit, gitRepository.Head.TipId);
			commit.Author = gitRepository.UserName ?? "";

			SetChildOfParents(commit);
			repository.Commits[commit.Id] = commit;
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


		private void CopyToCommit(GitCommit gitCommit, MCommit commit)
		{
			string subject = gitCommit.Subject;
			string tickets = GetTickets(subject);

			commit.Id = gitCommit.Id;
			commit.ShortId = gitCommit.ShortId;
			commit.Subject = GetSubjectWithoutTickets(subject, tickets);
			commit.Author = gitCommit.Author;
			commit.AuthorDate = gitCommit.AuthorDate;
			commit.CommitDate = gitCommit.CommitDate;
			commit.Tickets = tickets;
			commit.ParentIds = gitCommit.Parents.Select(c => c.Id).ToList();
		}

		private static void CopyToCommit(GitStatus gitStatus, MCommit commit, string parentId)
		{
			commit.Id = MCommit.UncommittedId;
			commit.ShortId = commit.Id.Substring(0, 6);
			commit.Subject = $"{gitStatus.Count} uncommitted changes in working folder";
			commit.Author = "";
			commit.AuthorDate = DateTime.Now;
			commit.CommitDate = DateTime.Now;
			commit.Tickets = "";
			commit.ParentIds = new List<string> { parentId };
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
