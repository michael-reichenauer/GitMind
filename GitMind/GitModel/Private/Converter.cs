using System.Linq;


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
				commit.CommitId,
				commit.ShortId,
				commit.Subject,
				commit.Author,
				commit.AuthorDate,
				commit.CommitDate,
				commit.Tags,
				commit.Tickets,
				commit.BranchTips,
				commit.ParentIds.ToList(),
				commit.Repository.ChildIds(commit.Id).ToList(),
				commit.BranchId,
				commit.SpecifiedBranchName,
				commit.CommitBranchName,
				commit.IsLocalAhead,
				commit.IsRemoteAhead,
				commit.IsUncommitted,
				commit.IsVirtual,
				commit.HasConflicts,
				commit.IsMerging,
				commit.HasFirstChild);
		}
	}
}