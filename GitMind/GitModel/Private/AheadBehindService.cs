using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Features.Branching.Private;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class AheadBehindService : IAheadBehindService
	{
		private readonly IGitBranchesService gitBranchesService;


		public AheadBehindService()
			: this(new GitBranchesService())
		{
		}

		public AheadBehindService(IGitBranchesService gitBranchesService)
		{
			this.gitBranchesService = gitBranchesService;
		}


		public void SetAheadBehind(MRepository repository)
		{
			IEnumerable<MBranch> localAndRemote = repository.Branches.Values
				.Where(b => b.IsActive && b.IsLocal && b.IsRemote)
				.ToList();

			IEnumerable<MBranch> localOnly = repository.Branches.Values
				.Where(b => b.IsActive && b.IsLocal && !b.IsRemote)
				.ToList();

			localOnly.ForEach(b =>
			{
				b.LocalAheadCount = b.Commits.Count();
				b.Commits.ForEach(c => c.IsLocalAhead = true);
			});

			localAndRemote.ForEach(b => b.IsLocalAndRemote = true);

			foreach (MBranch branch in localAndRemote)
			{
				string localTip = branch.LocalTipCommitId;
				string remoteTip = branch.RemoteTipCommitId;

				if (localTip == remoteTip)
				{
					branch.LocalAheadCount = 0;
					branch.RemoteAheadCount = 0;
				}
				else
				{
					R<GitDivergence> div = gitBranchesService.CheckAheadBehind(
						repository.WorkingFolder, localTip, remoteTip);
					if (div.HasValue)
					{
						branch.LocalAheadCount = div.Value.AheadBy;
						branch.RemoteAheadCount = div.Value.BehindBy;

						if (branch.LocalAheadCount > 0)
						{
							branch.LocalAheadCount = Math.Min(
								div.Value.AheadBy,
								branch.Commits
									.SkipWhile(c => c.Id != branch.LocalTipCommitId)
									.TakeWhile(c => c.Id != div.Value.CommonId)
									.Count());
							if (branch.LocalAheadCount <= div.Value.AheadBy)
							{
								branch.Commits
									.SkipWhile(c => c.Id != branch.LocalTipCommitId)
									.TakeWhile(c => c.Id != div.Value.CommonId)
									.ForEach(c => c.IsLocalAhead = true);
							}
						}

						if (branch.RemoteAheadCount > 0)
						{
							branch.RemoteAheadCount = Math.Min(
								branch.RemoteAheadCount,
								branch.Commits
									.SkipWhile(c => c.Id != branch.RemoteTipCommitId)
									.TakeWhile(c => c.Id != div.Value.CommonId)
									.Count());

							if (branch.RemoteAheadCount <= div.Value.BehindBy)
							{
								branch.Commits
									.SkipWhile(c => c.Id != branch.RemoteTipCommitId)
									.TakeWhile(c => c.Id != div.Value.CommonId)
									.ForEach(c => c.IsRemoteAhead = true);
							}
						}
					}
					else
					{
						branch.LocalAheadCount = 0;
						branch.RemoteAheadCount = 0;
					}
				}

				Log.Warn($"{branch.Name} has '{branch.LocalAheadCount}', '{branch.RemoteAheadCount}'");
			}
		}


		//private static void CountLocalAndRemoteCommits(IEnumerable<MBranch> branches)
		//{
		//	foreach (var branch in branches)
		//	{
		//		int localAheadCount = 0;
		//		int remoteAheadCount = 0;
		//		foreach (MCommit commit in branch.Commits)
		//		{
		//			if (commit.IsLocalAhead)
		//			{
		//				localAheadCount++;
		//			}
		//			else if (commit.IsRemoteAhead)
		//			{
		//				remoteAheadCount++;
		//			}
		//		}

		//		branch.LocalAheadCount = localAheadCount;
		//		branch.RemoteAheadCount = remoteAheadCount;
		//	}
		//}


		//private static void MarkRemoteCommits(IEnumerable<MBranch> branches)
		//{
		//	foreach (MBranch branch in branches)
		//	{
		//		MCommit commit = branch.Repository.Commits[branch.RemoteTipCommitId];

		//		Stack<MCommit> stack = new Stack<MCommit>();
		//		stack.Push(commit);

		//		while (stack.Any())
		//		{
		//			commit = stack.Pop();


		//			if (commit.BranchId == branch.Id && !commit.IsRemoteAheadMarker)
		//			{
		//				if (!commit.IsUncommitted)
		//				{
		//					commit.IsRemoteAheadMarker = true;
		//				}

		//				foreach (MCommit parent in commit.Parents)
		//				{
		//					stack.Push(parent);							
		//				}
		//			}
		//		}
		//	}
		//}


		//private static void MarkLocalCommits(IEnumerable<MBranch> branches)
		//{
		//	foreach (MBranch branch in branches)
		//	{
		//		MCommit commit = branch.Repository.Commits[branch.LocalTipCommitId];

		//		Stack<MCommit> stack = new Stack<MCommit>();
		//		stack.Push(commit);

		//		while (stack.Any())
		//		{
		//			commit = stack.Pop();

		//			if (commit.BranchId == branch.Id && !commit.IsLocalAheadMarker)
		//			{
		//				if (!commit.IsUncommitted)
		//				{
		//					commit.IsLocalAheadMarker = true;
		//				}

		//				foreach (MCommit parent in commit.Parents)
		//				{
		//					stack.Push(parent);							
		//				}
		//			}
		//		}
		//	}
		//}
	}
}