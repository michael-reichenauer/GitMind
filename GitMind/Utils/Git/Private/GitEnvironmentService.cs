using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Common.Tracking;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	[SingleInstance]
	internal class GitEnvironmentService : IGitEnvironmentService
	{
		private static readonly string GitVersion = "2.16.2.windows.1";
		private static string GitDefaultCmdPath => @"git.exe";

		private readonly ICmd2 cmd;

		private string gitCmdPath = null;

		public GitEnvironmentService(ICmd2 cmd)
		{
			this.cmd = cmd;
		}


		public string GetGitCmdPath()
		{
			string gitFolderPath = Path.Combine(ProgramPaths.DataFolderPath, "Git", GitVersion);
			string gitPath = Path.Combine(gitFolderPath, "cmd", "git.exe");

			if (!File.Exists(gitPath))
			{
				// Custom git exe not found, Using generic "git.exe" as git cmd path
				gitPath = GitDefaultCmdPath;
			}

			if (gitCmdPath != gitPath)
			{
				gitCmdPath = gitPath;

				string gitFullPath = TryGetGitCmdPathAsync(CancellationToken.None).Result;
				string gitVersion = null;
				if (!string.IsNullOrEmpty(gitFullPath))
				{
					gitVersion = TryGetGitVersionAsync(CancellationToken.None).Result;
				}

				Track.Info($"Using git: {gitFullPath}, Version: {gitVersion}");
			}

			return gitPath;
		}


		public async Task<string> TryGetWorkingFolderRootAsync(string path, CancellationToken ct)
		{
			if (path.EndsWith(".git"))
			{
				path = Path.GetDirectoryName(path);
			}

			CmdResult2 result = await cmd.RunAsync(
				GetGitCmdPath(), $"-C \"{path}\" rev-parse --show-toplevel", ct);

			return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output)
				? result.Output.Replace("/", "\\").Trim() : null;
		}


		public async Task<string> TryGetGitCorePathAsync(CancellationToken ct)
		{
			CmdResult2 result = await cmd.RunAsync(GetGitCmdPath(), "--exec-path", ct);

			return result.ExitCode == 0 ? result.Output.Trim() : null;
		}


		public async Task<string> TryGetGitCmdPathAsync(CancellationToken ct)
		{
			string corePath = await TryGetGitCorePathAsync(ct);

			if (string.IsNullOrEmpty(corePath))
			{
				return null;
			}

			string gitPath =
				Path.Combine(
					Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(corePath))),
				"cmd", "git.exe");

			if (!File.Exists(gitPath))
			{
				Log.Warn($"Expeded git cmd path not found: {gitPath}");
				return null;
			}

			return gitPath;
		}


		public async Task<string> TryGetGitVersionAsync(CancellationToken ct)
		{
			CmdResult2 result = await cmd.RunAsync(GetGitCmdPath(), "version", ct);

			return result.ExitCode == 0 && result.Output.StartsWithOic("git version ")
				? result.Output.Substring(12).Trim() : null;
		}
	}
}