using System.Linq;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class AheadBehindService : IAheadBehindService
	{
		public void SetAheadBehind(MRepository repository)
		{
			var bothLocalAndRemotebranches = repository.SubBranches.Where(b => b.IsActive)
				.GroupBy(b => b.BranchId)
				.Where(g => g.Count() == 2 && g.Any(b => !b.IsRemote) && g.Any(b => b.IsRemote))
				.Select(g => repository.Branches.First(b => b.Id == g.Key));

			bothLocalAndRemotebranches.ForEach(b => b.IsLocalAndRemote = true);

			Timing t = new Timing();
			var localBranches = repository.SubBranches.Where(b => b.IsActive && !b.IsRemote);
			localBranches.ForEach(branch => MarkIsLocalAhead(branch.LatestCommit));
			t.Log("Local commits");

			var remoteBranches = repository.SubBranches.Where(b => b.IsActive && b.IsRemote);
			remoteBranches.ForEach(branch => MarkIsRemoteAhead(branch.LatestCommit));
			t.Log("Remote commits");

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

			t.Log("Summery of local and remote commits");
		}


		private void MarkIsLocalAhead(MCommit commit)
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


		private void MarkIsRemoteAhead(MCommit commit)
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