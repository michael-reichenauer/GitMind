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
				branch.Commits.ForEach(c => c.IsLocalAhead = false);
				branch.Commits.ForEach(c => c.IsRemoteAhead = false);
				branch.Commits.ForEach(c => c.IsCommon = false);
				branch.LocalAheadCount = 0;
				branch.RemoteAheadCount = 0;

				string localTip = branch.LocalTipCommitId;
				if (localTip == Commit.UncommittedId)
				{
					localTip = branch.Repository.Commits[branch.LocalTipCommitId].FirstParentId;
				}

				string remoteTip = branch.RemoteTipCommitId;
				Log.Debug($"Local: {localTip}, remote: {remoteTip}");

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
						string commonTip = div.Value.CommonId;

						int localAheadBy = div.Value.AheadBy;
						int remoteAheadBy = div.Value.BehindBy;

						Log.Debug($"{branch.Name} has  Local: {localTip}, remote: {remoteTip}, Base: {commonTip}");
						Log.Debug($"{branch.Name} has  Local count: {localAheadBy}, remote count: {remoteAheadBy}");

						if (localAheadBy > 0 || remoteAheadBy > 0)
						{
							MCommit commit = branch.Repository.Commits[commonTip];
							commit.CommitAndFirstAncestors().ForEach(c => c.IsCommon = true);
						}

						if (localAheadBy > 0)
						{
							int localCount = 0;
							Stack<MCommit> commits = new Stack<MCommit>();
							commits.Push(branch.Repository.Commits[localTip]);

							while (commits.Any())
							{
								MCommit commit = commits.Pop();
								if (!commit.IsCommon && commit.Branch == branch)
								{
									commit.IsLocalAhead = true;
									localCount++;
									commit.Parents.Where(p => p.Branch == branch).ForEach(p => commits.Push(p));
								}
							}

							branch.LocalAheadCount = Math.Max(1, localCount);
						}

						if (remoteAheadBy > 0)
						{
							int remoteCount = 0;
							Stack<MCommit> commits = new Stack<MCommit>();
							commits.Push(branch.Repository.Commits[remoteTip]);

							while (commits.Any())
							{
								MCommit commit = commits.Pop();
								if (!commit.IsCommon && commit.Branch == branch)
								{
									commit.IsRemoteAhead = true;
									remoteCount++;
									commit.Parents.Where(p => p.Branch == branch).ForEach(p => commits.Push(p));
								}
							}

							branch.RemoteAheadCount = Math.Max(1, remoteCount);
						}
					}
					else
					{
						branch.LocalAheadCount = 1;
						branch.RemoteAheadCount = 1;
					}
				}

				Log.Debug($"{branch.Name} has '{branch.LocalAheadCount}', '{branch.RemoteAheadCount}'");
			}
		}
	}
}
