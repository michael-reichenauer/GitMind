using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitStatus : IGitStatus
	{
		private static readonly char[] Eol = "\n".ToCharArray();

		private readonly IGitCmd gitCmd;
		private static readonly string StatusArgs = "status -s";


		public GitStatus(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public async Task<Status> GetAsync(CancellationToken ct)
		{
			CmdResult2 result = await gitCmd.RunAsync(StatusArgs, ct);

			result.ThrowIfError("Failed to get status");

			string statusText = result.Output;
			Status status = ParseStatus(statusText);
			Log.Debug($"Status: {status}");
			return status;
		}


		private static Status ParseStatus(string status)
		{
			int modified = 0;
			int added = 0;
			int deleted = 0;
			int other = 0;

			string[] statusLines = status.Split(Eol);

			foreach (string line in statusLines)
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
				else if (!string.IsNullOrWhiteSpace(line))
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