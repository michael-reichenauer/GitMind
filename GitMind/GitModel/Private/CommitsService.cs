using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
			IEnumerable<CommitSha> rootCommits = gitRepository.Branches.Select(b => new CommitSha(b.TipId));

			if (gitRepository.Head.IsDetached)
			{
				rootCommits = rootCommits.Concat(new[] { new CommitSha(gitRepository.Head.TipId) });
			}

			if (!rootCommits.Any())
			{
				AddVirtualEmptyCommit(repository);
				rootCommits = new[] { CommitSha.NoCommits };
			}

			rootCommits = rootCommits.ToList();
			t.Log("Root commit ids");


			Dictionary<CommitSha, object> added = new Dictionary<CommitSha, object>();

			Dictionary<CommitId, BranchName> branchNameByCommitId = new Dictionary<CommitId, BranchName>();
			Dictionary<CommitId, BranchName> subjectBranchNameByCommitId = new Dictionary<CommitId, BranchName>();

			Stack<CommitSha> commitShas = new Stack<CommitSha>();
			rootCommits.ForEach(sha => commitShas.Push(sha));
			rootCommits.ForEach(sha => added[sha] = null);
			t.Log("Pushed roots on stack");

			while (commitShas.Any())
			{
				CommitSha commitSha = commitShas.Pop();
				CommitId commitId = new CommitId(commitSha.Sha);

				GitCommit gitCommit;
				IEnumerable<CommitSha> parentIds = null;
				if (!repository.GitCommits.TryGetValue(commitId, out gitCommit))
				{
					// This git commit id has not yet been seen before
					var gitLibCommit = gitRepository.GetCommit(commitSha);

					parentIds = gitLibCommit.ParentIds;

					gitCommit = ToGitCommit(gitLibCommit);

					if (IsMergeCommit(gitCommit))
					{
						TrySetBranchNameFromSubject(commitId, gitCommit, branchNameByCommitId, subjectBranchNameByCommitId);
					}

					repository.GitCommits[commitId] = gitCommit;
				}

				MCommit commit = repository.Commit(commitId);
				if (!commit.IsSet)
				{
					if (commit.Id == CommitId.NoCommits)
					{
						commit.IsVirtual = true;
						commit.SetBranchName("master");
					}

					AddCommit(commit, gitCommit);

					if (parentIds == null)
					{
						parentIds = gitCommit.ParentIds.Select(id => repository.GitCommits[id].Sha);
					}

					AddParents(parentIds, commitShas, added);
				}

				BranchName branchName;
				if (branchNameByCommitId.TryGetValue(commitId, out branchName))
				{
					// Branch name set by a child commit (pull merge commit)
					commit.SetBranchName(branchName);
				}

				BranchName subjectBranchName;
				if (subjectBranchNameByCommitId.TryGetValue(commitId, out subjectBranchName))
				{
					// Subject branch name set by a child commit (merge commit)
					gitCommit.SetBranchNameFromSubject(subjectBranchName);
				}
			}

			if (!status.IsOK)
			{
				// Adding a virtual "uncommitted" commit since current working folder status has changes
				AddVirtualUncommitted(gitRepository, status, repository);
			}
		}


		private static GitCommit ToGitCommit(GitLibCommit gitLibCommit)
		{
			List<CommitId> parentIds = gitLibCommit.ParentIds
				.Select(sha => new CommitId(sha.Sha))
				.ToList();

			return new GitCommit(
				gitLibCommit.Sha,
				gitLibCommit.Subject,
				gitLibCommit.Author,
				gitLibCommit.AuthorDate,
				gitLibCommit.CommitDate,
				parentIds);
		}


		private void AddCommit(MCommit commit, GitCommit gitCommit)
		{
			//string subject = gitCommit.Subject;
			string tickets = ""; // GetTickets(subject);
			commit.Tickets = tickets;

			// Pre-create all parents
			commit.ParentIds.ForEach(pid => commit.Repository.Commit(pid));

			SetChildOfParents(commit);
			commit.IsSet = true;
		}


		private void AddVirtualUncommitted(
			GitRepository gitRepository, Status status, MRepository repository)
		{
			MCommit commit = repository.Commit(CommitId.Uncommitted);
			repository.Uncommitted = commit;

			commit.IsVirtual = true;

			CommitId headCommitId = CommitId.NoCommits;

			if (gitRepository.Head.HasCommits)
			{
				CommitId headId = new CommitId(gitRepository.Head.TipId);
				MCommit headCommit = repository.Commit(headId);
				headCommitId = headCommit.Id;
			}

			CopyToUncommitedCommit(gitRepository, repository, status, commit, headCommitId);

			SetChildOfParents(commit);
		}


		private void AddVirtualEmptyCommit(MRepository repository)
		{
			CommitSha virtualSha = CommitSha.NoCommits;
			CommitId virtualId = new CommitId(virtualSha);

			//MCommit commit = new MCommit()
			//{
			//	Repository = repository,
			//	Id = virtualId,
			//};

			//repository.Commits[virtualId] = commit;

			//commit.IsVirtual = true;

			GitCommit gitCommit = new GitCommit(
				virtualSha,
				"<Repository with no committs yet ...>",
				"",
				DateTime.Now,
				DateTime.Now,
				new List<CommitId>());

			repository.GitCommits[virtualId] = gitCommit;
		}


		private static void AddParents(
			IEnumerable<CommitSha> parents,
			Stack<CommitSha> commitShas,
			Dictionary<CommitSha, object> added)
		{
			parents.ForEach(parent =>
			{
				if (!added.ContainsKey(parent))
				{
					commitShas.Push(parent);
					added[parent] = null;
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


		private static void CopyToUncommitedCommit(
			GitRepository gitRepository,
			MRepository repository,
			Status status,
			MCommit commit,
			CommitId parentId)
		{
			int modifiedCount = status.ChangedCount;
			int conflictCount = status.ConflictCount;

			string subject = $"{modifiedCount} uncommitted changes in working folder";

			if (conflictCount > 0)
			{
				subject =
					$"{conflictCount} conflicts and {modifiedCount} changes, {ShortSubject(status)}";
				commit.HasConflicts = true;
			}
			else if (status.IsMerging)
			{
				subject = $"{modifiedCount} changes, {ShortSubject(status)}";
				commit.IsMerging = true;
			}

			GitCommit gitCommit = new GitCommit(
				CommitSha.Uncommitted,
				subject,
				gitRepository.UserName ?? "",
				DateTime.Now,
				DateTime.Now,
				new List<CommitId> { parentId });

			repository.GitCommits[CommitId.Uncommitted] = gitCommit;

			commit.SetBranchName(gitRepository.Head.Name);

			commit.Tickets = "";
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


		//private static Regex rgx1 = new Regex(@"([\,; ]*#(\d\d*)[\,; ]*)|([\,; ]*#CST(\d\d*)[\,; ]*)");
		//private static Regex rgx2 = new Regex(@"[\,; ]*#(CST\d\d*)[\,; ]*");

		//private static Regex rgx1 = new Regex(@"#(\d\d*)");

		//private string GetTickets(string subject)
		//{
		//	string tickets = "";
		//	foreach (Match match in rgx1.Matches(subject))
		//	{
		//		tickets += match.Value;
		//	}

		
		//	return tickets;
		//}
	}
}
