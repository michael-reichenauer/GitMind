using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Common.Tracking;
using GitMind.MainWindowViews;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitCmd : IGitCmd
	{
		private readonly ICmd2 cmd;
		private readonly IGitEnvironmentService gitEnvironmentService;
		private readonly WindowOwner owner;
		private readonly WorkingFolderPath workingFolder;


		public GitCmd(
			ICmd2 cmd,
			IGitEnvironmentService gitEnvironmentService,
			WindowOwner owner,
			WorkingFolderPath workingFolder)
		{
			this.cmd = cmd;
			this.gitEnvironmentService = gitEnvironmentService;
			this.owner = owner;
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
			//if (options.IsEnableCredentials)
			{
				using (CredentialSession session = new CredentialSession(owner))
				{
					//// Enable credentials handling
					//gitArgs = $"-c credential.helper =\"!GitMind.exe --cmg {session.Id}\" { gitArgs}";

					GitResult gitResult = await RunGitCmsAsync(gitArgs, options, ct);

					bool isValidCredentials =
						!(gitResult.ExitCode == 128 &&
						-1 != gitResult.Error.IndexOf("Authentication failed", StringComparison.OrdinalIgnoreCase));

					session.ConfirmValidCrededntial(isValidCredentials);

					return gitResult;
				}
			}
			//else
			//{
			//	return await RunGitCmsAsync(gitArgs, options, ct);
			//}
		}



		private async Task<GitResult> RunGitCmsAsync(
			string gitArgs, GitOptions options, CancellationToken ct)
		{
			AdjustOptions(options);

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
				// If git needs to ask for command line credentials, redirect that to GitMind exe to answer
				string instanceDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				environment["Path"] = $"{instanceDir};{environment["Path"]}";
				environment["GIT_ASKPASS"] = @"GitMind";
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