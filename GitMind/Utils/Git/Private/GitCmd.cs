using System;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitCmd : IGitCmd
	{
		// git config --list --show-origin
		private static string GitCmdPath => @"C:\Work Files\MinGit\cmd\git.exe";

		private readonly ICmd2 cmd;
		private readonly WorkingFolderPath workingFolder;


		public GitCmd(ICmd2 cmd, WorkingFolderPath workingFolder)
		{
			this.cmd = cmd;
			this.workingFolder = workingFolder;
		}


		public async Task<CmdResult2> RunAsync(
			string gitArgs, CmdOptions options, CancellationToken ct)
		{
			options.WorkingDirectory = options.WorkingDirectory ?? workingFolder;

			return await CmdAsync(gitArgs, options, ct);
		}


		public async Task<CmdResult2> RunAsync(string gitArgs, CancellationToken ct)
		{
			CmdOptions options = new CmdOptions { WorkingDirectory = workingFolder };

			return await CmdAsync(gitArgs, options, ct);
		}


		public async Task<CmdResult2> RunAsync(
			string gitArgs, Action<string> outputLines, CancellationToken ct)
		{
			CmdOptions options = new CmdOptions
			{
				WorkingDirectory = workingFolder,
				OutputLines = outputLines,
				IsOutputDisabled = true,
			};

			return await CmdAsync(gitArgs, options, ct);
		}


		private async Task<CmdResult2> CmdAsync(
			string gitArgs, CmdOptions options, CancellationToken ct)
		{
			Timing t = Timing.StartNew();
			CmdResult2 result = await cmd.RunAsync(GitCmdPath, gitArgs, options, ct);
			Log.Debug($"{t.ElapsedMs}ms: {result}");
			return result;
		}
	}
}