using System.Collections.Generic;
using System.Linq;


namespace GitMind.GitModel.Private
{
	internal class AheadBehindService : IAheadBehindService
	{
		public void SetAheadBehind(MRepository repository)
		{
			// Getting all branches, which are active and have both local and remote tracking branches.
			IEnumerable<MSubBranch> activeSubBranches = GetActiveSubBranches(repository);
			IEnumerable<MBranch> bothLocalAndRemoteBranches = GetBranchesWhichAreBothLocalAndRemote(
				repository, activeSubBranches);

			bothLocalAndRemoteBranches.ForEach(b => b.IsLocalAndRemote = true);

			// Mark all commits in local branches as local commit
			MarkLocalCommits(repository);

			// Mark all commits in remote branches as remote commits
			MarkRemoteCommits(repository);

			// Count all commits in all branches to get the local and remote ahead count for a branch 
			CountLocalAndRemoteCommits(repository);
		}


		private static IEnumerable<MSubBranch> GetActiveSubBranches(MRepository repository)
		{
			return repository.SubBranches.Where(b => b.Value.IsActive).Select(b => b.Value);
		}


		private static IEnumerable<MBranch> GetBranchesWhichAreBothLocalAndRemote(
			MRepository repository, IEnumerable<MSubBranch> activeSubBranches)
		{
			return activeSubBranches
				.GroupBy(b => b.BranchId)
				.Where(g => g.Count() == 2 && g.Any(b => b.IsLocal) && g.Any(b => b.IsRemote))
				.Select(g => repository.Branches.First(b => b.Value.Id == g.Key).Value);
		}


		private static void CountLocalAndRemoteCommits(MRepository repository)
		{
			foreach (var branch in repository.Branches)
			{
				int localAheadCount = 0;
				int remoteAheadCount = 0;
				foreach (MCommit commit in branch.Value.Commits)
				{
					if (commit.IsLocalAhead)
					{
						localAheadCount++;
					}
					else if (commit.IsRemoteAhead)
					{
						remoteAheadCount++;
					}
				}

				branch.Value.LocalAheadCount = localAheadCount;
				branch.Value.RemoteAheadCount = remoteAheadCount;
			}
		}


		private static void MarkRemoteCommits(MRepository repository)
		{
			var remoteSubBranches = repository.SubBranches.Where(b => b.Value.IsActive && b.Value.IsRemote);
			remoteSubBranches.ForEach(branch => MarkIsRemoteAhead(branch.Value.LatestCommit));
		}


		private static void MarkLocalCommits(MRepository repository)
		{
			var localSubBranches = repository.SubBranches.Where(b => b.Value.IsActive && b.Value.IsLocal);
			localSubBranches.ForEach(branch => MarkIsLocalAhead(branch.Value.LatestCommit));
		}


		private static void MarkIsLocalAhead(MCommit commit)
		{
			if (!commit.IsLocalAheadMarker)
			{
				if (!commit.IsUncommitted)
				{
					commit.IsLocalAheadMarker = true;
				}

				foreach (MCommit parent in commit.Parents)
				{
					MarkIsLocalAhead(parent);
				}
			}
		}


		private static void MarkIsRemoteAhead(MCommit commit)
		{
			if (!commit.IsRemoteAheadMarker)
			{
				commit.IsRemoteAheadMarker = true;

				foreach (MCommit parent in commit.Parents)
				{
					MarkIsRemoteAhead(parent);
				}
			}
		}
	}
}