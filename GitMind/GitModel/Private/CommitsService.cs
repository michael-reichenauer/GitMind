using System.Collections.Generic;
using System.Linq;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	internal class CommitsService : ICommitsService
	{
		public void AddBranchCommits(IGitRepo gitRepo, MRepository repository)
		{
			IEnumerable<string> rootCommitIds = gitRepo.GetAllBranches().Select(b => b.LatestCommitId);
			Dictionary<string, string> branchNameByCommitId = new Dictionary<string, string>();
			Dictionary<string, string> subjectBranchNameByCommitId = new Dictionary<string, string>();
			Dictionary<string, object> added = new Dictionary<string, object>();

			Stack<string> commitIds = new Stack<string>();
			rootCommitIds.ForEach(id => commitIds.Push(id));
			rootCommitIds.ForEach(id => added[id] = null);

			while (commitIds.Any())
			{
				string commitId = commitIds.Pop();

				MCommit commit;
				if (!repository.TryGetCommit(commitId, out commit))
				{
					commit = AddCommit(commitId, gitRepo, repository);

					if (IsMergeCommit(commit))
					{
						TrySetBranchNameFromSubject(commit, branchNameByCommitId, subjectBranchNameByCommitId);
					}

					AddParents(commit, commitIds, added);
				}

				string branchName;
				if (branchNameByCommitId.TryGetValue(commitId, out branchName))
				{
					// Branch name set by a child commit (pull merge commit)
					commit.BranchName = branchName;
				}

				string subjectBranchName;
				if (subjectBranchNameByCommitId.TryGetValue(commitId, out subjectBranchName))
				{
					// Subject branch name set by a child commit (merge commit)
					commit.FromSubjectBranchName = subjectBranchName;
				}
			}
		}


		private MCommit AddCommit(string commitId, IGitRepo gitRepo, MRepository repository)
		{
			GitCommit gitCommit = gitRepo.GetCommit(commitId);

			MCommit commit = new MCommit();
			commit.Repository = repository;

			CopyToCommit(gitCommit, commit);
			SetChildOfParents(commit);
			repository.AddCommit(commit);
			return commit;
		}


		private static void AddParents(MCommit commit, Stack<string> commitIds, Dictionary<string, object> added)
		{
			commit.ParentIds.ForEach(parentId =>
			{
				if (!added.ContainsKey(parentId))
				{
					commitIds.Push(parentId);
					added[parentId] = null;
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
			string tickets = GetTickets(gitCommit);

			commit.Id = gitCommit.Id;
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
