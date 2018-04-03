﻿using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitStatus : IGitStatus
	{
		private readonly IGitCmd gitCmd;
		private static readonly string StatusArgs = "status -s --porcelain";


		public GitStatus(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public async Task<R<Status>> GetAsync(CancellationToken ct)
		{
			CmdResult2 result = await gitCmd.RunAsync(StatusArgs, ct);

			if (result.IsFaulted)
			{
				return Error.From(result.Error);
			}

			Status status = ParseStatus(result);
			Log.Debug($"Status: {status}");
			return status;
		}


		private static Status ParseStatus(CmdResult2 result)
		{
			int modified = 0;
			int added = 0;
			int deleted = 0;
			int other = 0;


			foreach (string line in result.OutputLines)
			{
				if (line.StartsWith(" M "))
				{
					modified++;
				}
				else if (line.StartsWith("?? "))
				{
					added++;
				}
				else if (line.StartsWith(" D "))
				{
					deleted++;
				}
				else
				{
					other++;
				}
			}

			return new Status(modified, added, deleted, other);
		}
	}


	internal class Status
	{
		public Status(int modified, int added, int deleted, int other)
		{
			Modified = modified;
			Added = added;
			Deleted = deleted;
			Other = other;
		}

		public int Modified { get; }
		public int Added { get; }
		public int Deleted { get; }
		public int Other { get; }
		public int Changed => Modified + Added + Deleted + Other;

		public bool OK => Changed == 0;

		public override string ToString() =>
			$"Changed {Changed} ({Modified}M, {Added}A, {Deleted}D, {Other}?)";
	}
}