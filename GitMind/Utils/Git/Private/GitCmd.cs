using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Common.Tracking;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitCmd : IGitCmd
	{
		// git config --list --show-origin
		//private static readonly string CredentialsConfig = @"-c credential.helper=!GitMind.exe";

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


		public async Task<GitResult> RunAsync(
			string gitArgs, GitOptions options, CancellationToken ct)
		{
			return await CmdAsync(gitArgs, options, ct);
		}


		public async Task<GitResult> RunAsync(string gitArgs, CancellationToken ct)
		{
			return await CmdAsync(gitArgs, new GitOptions(), ct);
		}


		public async Task<GitResult> RunAsync(
			string gitArgs, Action<string> outputLines, CancellationToken ct)
		{
			GitOptions options = new GitOptions
			{
				OutputLines = outputLines,
				IsOutputDisabled = true,
			};

			return await CmdAsync(gitArgs, options, ct);
		}


		private async Task<GitResult> CmdAsync(
			string gitArgs, GitOptions options, CancellationToken ct)
		{
			AdjustOptions(options);

			// Enable credentials handling
			//gitArgs = $"{CredentialsConfig} {gitArgs}";

			Timing t = Timing.StartNew();
			Log.Debug($"Runing: {GitCmdPath} {gitArgs}");
			CmdOptions cmdOptions = ToCmdOptions(options);
			CmdResult2 result = await cmd.RunAsync(GitCmdPath, gitArgs, cmdOptions, ct);
			Track.Event("gitCmd", $"{t.ElapsedMs}ms: {result.ToStringShort()}");

			if (result.ExitCode == 0)
			{
				Log.Debug($"{t.ElapsedMs}ms: {result}");
			}
			else
			{
				Log.Warn($"{t.ElapsedMs}ms: {result}");
			}

			return new GitResult(result);
		}


		private void AdjustOptions(GitOptions options)
		{
			options.WorkingDirectory = options.WorkingDirectory ?? workingFolder;

			// Used to enable credentials handling
			options.EnvironmentVariables = environment =>
			{
				string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				environment["Path"] = $"{dir};{environment["Path"]}";

				string askPath = @"C:\Work Files\GitMindAsk\GitMindAsk\bin\Debug";
				environment["Path"] = $"{askPath};{environment["Path"]}";

				environment["GIT_ASKPASS"] = @"GitMindAsk";
			};
		}


		private static CmdOptions ToCmdOptions(GitOptions options) => new CmdOptions()
		{
			InputText = options.InputText,
			OutputLines = options.OutputLines,
			ErrorLines = options.ErrorLines,
			IsErrortDisabled = options.IsErrortDisabled,
			EnvironmentVariables = options.EnvironmentVariables,
			WorkingDirectory = options.WorkingDirectory,
			IsOutputDisabled = options.IsOutputDisabled,
			ErrorProgress = options.ErrorProgress
		};
	}
}