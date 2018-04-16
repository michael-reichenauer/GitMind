using System;
using System.IO;
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

				string gitFullPath = TryGetGitCmdPath();
				string gitVersion = null;
				if (!string.IsNullOrEmpty(gitFullPath))
				{
					gitVersion = TryGetGitVersion();
				}

				Track.Info($"Using git: {gitFullPath}, Version: {gitVersion}");
			}

			return gitPath;
		}


		public string TryGetWorkingFolderRoot(string path)
		{
			if (path.EndsWith(".git"))
			{
				path = Path.GetDirectoryName(path);
			}

			string workingFolderRoot = null;

			while (!string.IsNullOrEmpty(path))
			{
				string gitRepoPath = Path.Combine(path, ".git");
				if (Directory.Exists(gitRepoPath))
				{

					workingFolderRoot = path.Replace("/", "\\").Trim();
					break;
				}

				path = Path.GetDirectoryName(path);
			}

			Log.Info($"Working folder: {workingFolderRoot}");
			return workingFolderRoot;
		}

		//public async Task<string> TryGetWorkingFolderRootAsync(string path, CancellationToken ct)
		//{
		//	if (path.EndsWith(".git"))
		//	{
		//		path = Path.GetDirectoryName(path);
		//	}

		//	CmdResult2 result = await cmd.RunAsync(
		//		GetGitCmdPath(), $"-C \"{path}\" rev-parse --show-toplevel", ct);
		//	Log.Debug($"cmd: {result.ElapsedMs} ms: {result}");
		//	string folderPath = result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output)
		//		? result.Output.Replace("/", "\\").Trim() : null;

		//	return folderPath;
		//}


		public string TryGetGitCorePath()
		{
			CmdResult2 result = cmd.Run(GetGitCmdPath(), "--exec-path");

			return result.ExitCode == 0 ? result.Output.Trim() : null;
		}


		public string TryGetGitCmdPath()
		{
			string corePath = TryGetGitCorePath();

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


		public string TryGetGitVersion()
		{
			CmdResult2 result = cmd.Run(GetGitCmdPath(), "version");

			return result.ExitCode == 0 && result.Output.StartsWithOic("git version ")
				? result.Output.Substring(12).Trim() : null;
		}
	}
}