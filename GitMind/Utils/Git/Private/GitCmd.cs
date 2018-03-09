using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitCmd : IGitCmd
	{
		// git config --list --show-origin
		private static readonly string CredentialsConfig =
			@"-c credential.helper=!GitMind.exe";

		private readonly ICmd2 cmd;
		private readonly IGitEnvironmentService gitEnvironmentService;
		private readonly WorkingFolderPath workingFolder;


		public GitCmd(
			ICmd2 cmd,
			IGitEnvironmentService gitEnvironmentService,
			WorkingFolderPath workingFolder)
		{
			this.cmd = cmd;
			this.gitEnvironmentService = gitEnvironmentService;
			this.workingFolder = workingFolder;
		}


		private string GitCmdPath => gitEnvironmentService.GetGitCmdPath();


		public async Task<CmdResult2> RunAsync(
			string gitArgs, CmdOptions options, CancellationToken ct)
		{
			return await CmdAsync(gitArgs, options, ct);
		}


		public async Task<CmdResult2> RunAsync(string gitArgs, CancellationToken ct)
		{
			return await CmdAsync(gitArgs, new CmdOptions(), ct);
		}


		public async Task<CmdResult2> RunAsync(
			string gitArgs, Action<string> outputLines, CancellationToken ct)
		{
			CmdOptions options = new CmdOptions
			{
				OutputLines = outputLines,
				IsOutputDisabled = true,
			};

			return await CmdAsync(gitArgs, options, ct);
		}


		private async Task<CmdResult2> CmdAsync(
			string gitArgs, CmdOptions options, CancellationToken ct)
		{
			AdjustOptions(options);

			// Enable credentials handling
			gitArgs = $"{CredentialsConfig} {gitArgs}";

			Timing t = Timing.StartNew();

			CmdResult2 result = await cmd.RunAsync(GitCmdPath, gitArgs, options, ct);
			Log.Debug($"{t.ElapsedMs}ms: {result}");
			return result;
		}


		private void AdjustOptions(CmdOptions options)
		{
			options.WorkingDirectory = options.WorkingDirectory ?? workingFolder;

			// Used to enable credentials handling
			options.EnvironmentVariables = environment =>
			{
				string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				environment["Path"] = $"{dir};{environment["Path"]}";
			};
		}
	}
}