using System.Linq;
using GitMind.Common;


namespace GitMind.GitModel.Private
{
	internal class Converter 
	{
		public static Branch ToBranch(Repository repository, MBranch branch)
		{
			return new Branch(
				repository,
				branch.Id,
				branch.Name,
				branch.TipCommitId,
				branch.FirstCommitId,
				branch.ParentCommitId,
				branch.Commits.Select(c => c.Id).ToList(),
				branch.ParentBranchId,
				branch.ChildBranchNames.ToList(),
				branch.MainBranchId,
				branch.LocalSubBranchId,
				branch.IsActive,
				branch.IsLocal,
				branch.IsRemote,
				branch.IsMainPart,
				branch.IsLocalPart,
				branch.IsMultiBranch,
				branch.IsDetached,
				branch.LocalAheadCount,
				branch.RemoteAheadCount);
		}

		public static Commit ToCommit(Repository repository, MCommit commit)
		{
			return new Commit(
				repository,
				commit.Id,
				commit.RealCommitId,
				commit.RealCommitSha,
				GetSubjectWithoutTickets(commit.Subject, commit.Tickets),
				commit.Author,
				commit.AuthorDate,
				commit.CommitDate,
				commit.Tags,
				commit.Tickets,
				commit.BranchTips,
				commit.ParentIds.ToList(),
				commit.ChildIds.ToList(),
				commit.BranchId,
				commit.SpecifiedBranchName,
				commit.CommitBranchName,
				commit.IsLocalAhead,
				commit.IsRemoteAhead,
				commit.IsCommon,
				commit.IsUncommitted,
				commit.IsVirtual,
				commit.HasConflicts,
				commit.IsMerging,
				commit.HasFirstChild);
		}

		private static string GetSubjectWithoutTickets(string subject, string tickets)
		{
			if ((subject?.Length ?? 0) < (tickets?.Length ?? 0))
			{
				return subject;
			}

			return subject?.Substring(tickets?.Length ?? 0);
		}
	}
}