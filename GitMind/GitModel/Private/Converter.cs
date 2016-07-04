using System.Linq;
using GitMind.Git;


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
				branch.LatestCommitId,
				branch.FirstCommitId,
				branch.ParentCommitId,
				branch.Commits.Select(c => c.Id).ToList(),			
				branch.ParentBranchId,
				branch.ChildBranchNames.ToList(),
				branch.IsActive,
				branch.IsMultiBranch,
				branch.LocalAheadCount,
				branch.RemoteAheadCount);
		}



		public static Commit ToCommit(Repository repository, MCommit commit)
		{
			return new Commit(
				repository,
				commit.Id,
				commit.ShortId,
				commit.Subject,
				commit.Author,
				commit.AuthorDate,
				commit.CommitDate,
				commit.Tags,
				commit.Tickets,
				commit.ParentIds.ToList(),
				commit.ChildIds.ToList(),
				commit.BranchId,
				commit.BranchNameSpecified,
				commit.IsLocalAhead,
				commit.IsRemoteAhead);
		}


		public static CommitFile ToCommitFile(GitFile gitFile)
		{
			string status = gitFile.IsAdded
				? "a"
				: gitFile.IsDeleted
					? "d"
					: gitFile.IsModified
						? "m"
						: gitFile.IsRenamed
							? "r"
							: "";

			return new CommitFile(gitFile.File, status);
		}
	}
}