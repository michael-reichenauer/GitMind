using System.Collections.Generic;
using System.Linq;


namespace GitMind.GitModel.Private
{
	internal class AheadBehindService : IAheadBehindService
	{
		public void SetAheadBehind(MRepository repository)
		{
			IEnumerable<MBranch> localAndRemote = repository.Branches.Values
				.Where(b => b.IsActive && b.IsLocal && b.IsRemote)
				.ToList();

			IEnumerable<MBranch> localOnly = repository.Branches.Values
				.Where(b => b.IsActive && b.IsLocal && !b.IsRemote)
				.ToList();

			localAndRemote.ForEach(b => b.IsLocalAndRemote = true);

			// Mark all commits in local branches as local commit
			IEnumerable<MBranch> branches = localAndRemote.Concat(localOnly).ToList();
			MarkLocalCommits(branches);

			// Mark all commits in remote branches as remote commits
			MarkRemoteCommits(localAndRemote);

			// Count all commits in branches to get the local and remote ahead count for a branch 
			CountLocalAndRemoteCommits(branches);
		}


		private static void CountLocalAndRemoteCommits(IEnumerable<MBranch> branches)
		{
			foreach (var branch in branches)
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


		private static void MarkRemoteCommits(IEnumerable<MBranch> branches)
		{
			foreach (MBranch branch in branches)
			{
				MCommit commit = branch.Repository.Commits[branch.RemoteTipCommitId];

				Stack<MCommit> stack = new Stack<MCommit>();
				stack.Push(commit);

				while (stack.Any())
				{
					commit = stack.Pop();


					if (commit.BranchId == branch.Id && !commit.IsRemoteAheadMarker)
					{
						if (!commit.IsUncommitted)
						{
							commit.IsRemoteAheadMarker = true;
						}

						foreach (MCommit parent in commit.Parents)
						{
							stack.Push(parent);							
						}
					}
				}
			}
		}


		private static void MarkLocalCommits(IEnumerable<MBranch> branches)
		{
			foreach (MBranch branch in branches)
			{
				MCommit commit = branch.Repository.Commits[branch.LocalTipCommitId];

				Stack<MCommit> stack = new Stack<MCommit>();
				stack.Push(commit);

				while (stack.Any())
				{
					commit = stack.Pop();

					if (commit.BranchId == branch.Id && !commit.IsLocalAheadMarker)
					{
						if (!commit.IsUncommitted)
						{
							commit.IsLocalAheadMarker = true;
						}

						foreach (MCommit parent in commit.Parents)
						{
							stack.Push(parent);							
						}
					}
				}
			}
		}
	}
}