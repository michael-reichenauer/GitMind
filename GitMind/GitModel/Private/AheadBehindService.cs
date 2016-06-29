using System.Collections.Generic;
using System.Linq;


namespace GitMind.GitModel.Private
{
	internal class AheadBehindService : IAheadBehindService
	{
		public void SetAheadBehind(MRepository repository)
		{
			// Getting all branches, which are active and have both local and remote tracking branches.
			IEnumerable<MSubBranch> activeSubBranches = repository.SubBranches.Where(b => b.IsActive);
			IEnumerable<MBranch> bothLocalAndRemotebranches = activeSubBranches
				.GroupBy(b => b.BranchId)
				.Where(g => g.Count() == 2 && g.Any(b => b.IsLocal) && g.Any(b => b.IsRemote))
				.Select(g => repository.Branches.First(b => b.Id == g.Key));

			bothLocalAndRemotebranches.ForEach(b => b.IsLocalAndRemote = true);

			// Mark all commits in local branches as local commit
			var localSubBranches = repository.SubBranches.Where(b => b.IsActive && b.IsLocal);
			localSubBranches.ForEach(branch => MarkIsLocalAhead(branch.LatestCommit));

			// Mark all commits in remote branches as remote commits
			var remoteSubBranches = repository.SubBranches.Where(b => b.IsActive && b.IsRemote);
			remoteSubBranches.ForEach(branch => MarkIsRemoteAhead(branch.LatestCommit));

			// Count all commits in all branches to get the local and remote ahead count for a branch 
			foreach (MBranch branch in repository.Branches)
			{
				int localAheadCount = 0;
				int remoteAheadCount = 0;
				foreach (MCommit commit in branch.Commits)
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

				branch.LocalAheadCount = localAheadCount;
				branch.RemoteAheadCount = remoteAheadCount;
			}
		}


		private static void MarkIsLocalAhead(MCommit commit)
		{
			if (!commit.IsLocalAheadMarker)
			{
				commit.IsLocalAheadMarker = true;

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