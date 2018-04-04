using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Common.MessageDialogs;
using GitMind.Common.Tracking;
using GitMind.Utils.Git.Private.CredentialsHandling;
using GitMind.Utils.OsSystem;
using GitMind.Utils.UI;


namespace GitMind.Utils.Git.Private
{
	internal class GitCmd : IGitCmd
	{
		private readonly ICmd2 cmd;
		private readonly IGitEnvironmentService gitEnvironmentService;
		private readonly ICredentialService credentialService;
		private readonly IMessage message;
		private readonly WorkingFolderPath workingFolder;


		public GitCmd(
			ICmd2 cmd,
			IGitEnvironmentService gitEnvironmentService,
			ICredentialService credentialService,
			IMessage message,
			WorkingFolderPath workingFolder)
		{
			this.cmd = cmd;
			this.gitEnvironmentService = gitEnvironmentService;
			this.credentialService = credentialService;
			this.message = message;
			this.workingFolder = workingFolder;
		}


		private string GitCmdPath => gitEnvironmentService.GetGitCmdPath();


		public async Task<R<CmdResult2>> RunAsync(
			string gitArgs, GitOptions options, CancellationToken ct)
		{
			return await CmdAsync(gitArgs, options, ct);
		}


		public async Task<R<CmdResult2>> RunAsync(string gitArgs, CancellationToken ct)
		{
			return await CmdAsync(gitArgs, new GitOptions(), ct);
		}


		public async Task<R<CmdResult2>> RunAsync(
			string gitArgs, Action<string> outputLines, CancellationToken ct)
		{
			GitOptions options = new GitOptions
			{
				OutputLines = outputLines,
				IsOutputDisabled = true,
			};

			return await CmdAsync(gitArgs, options, ct);
		}


		private async Task<R<CmdResult2>> CmdAsync(
			string gitArgs, GitOptions options, CancellationToken ct)
		{
			CmdResult2 result;
			bool isRetry = false;
			string username = null;
			do
			{
				using (CredentialSession session = new CredentialSession(credentialService, username))
				{
					result = await RunGitCmsAsync(gitArgs, options, session.Id, ct);

					username = session.Username;
					session.ConfirmValidCrededntial(!IsAuthenticationFailed(result));
					isRetry = IsAuthenticationFailed(result) &&
										session.IsCredentialRequested &&
										!session.IsAskPassCanceled;

					if (isRetry)
					{
						UiThread.Run(() => message.ShowError($"Invalid credentials for {session.TargetUri}"));
					}
				}
			} while (isRetry);

			if (result.IsFaulted)
			{
				return Error.From($"{result.Error}:\n{result}");
			}

			return result;
		}


		private async Task<CmdResult2> RunGitCmsAsync(
			string gitArgs, GitOptions options, string sessionId, CancellationToken ct)
		{
			AdjustOptions(options, sessionId);

			// Log.Debug($"Runing: {GitCmdPath} {gitArgs}");
			CmdOptions cmdOptions = ToCmdOptions(options);
			CmdResult2 result = await cmd.RunAsync(GitCmdPath, gitArgs, cmdOptions, ct);

			if (result.IsFaulted && !result.IsCanceled)
			{
				Track.Event("gitCmd", $"{result.ElapsedMs}ms: Exit {result.ExitCode}: {result.Command} {result.Arguments}");
				Log.Warn($"{result.ElapsedMs}ms: {result}");
			}
			else
			{
				Track.Event("gitCmd", $"{result.ElapsedMs}ms: {result.Command} {result.Arguments}");
				Log.Debug($"{result}");
			}

			return result;
		}



		private static bool IsAuthenticationFailed(CmdResult2 result) =>
			result.ExitCode == 128 && -1 != result.Error.IndexOfOic("Authentication failed");


		private void AdjustOptions(GitOptions options, string sessionId)
		{
			options.WorkingDirectory = options.WorkingDirectory ?? workingFolder;

			// Used to enable credentials handling
			options.EnvironmentVariables = environment =>
			{
				// If git needs to ask for command line credentials, redirect that to GitMind to answer
				string instanceDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				environment["Path"] = $"{instanceDir};{environment["Path"]}";
				environment["GIT_ASKPASS"] = "GitMind";
				environment["GITMIND_SESSIONID"] = sessionId;
			};
		}


		private static CmdOptions ToCmdOptions(GitOptions options) => new CmdOptions()
		{
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