using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Common.Tracking;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	[SingleInstance]
	internal class GitEnvironmentService : IGitEnvironmentService
	{
		private static string GitDefaultCmdPath => @"git.exe";

		//private static readonly string GitVersion = "2.16.2.windows.1";
		//private static string gitUri =
		//	"https://github.com/git-for-windows/git/releases/download/v2.16.2.windows.1/MinGit-2.16.2-64-bit.zip";

		public static readonly string GitVersion = "2.17.0.windows.1";
		private static string gitUri =
			"https://github.com/git-for-windows/git/releases/download/v2.17.0.windows.1/MinGit-2.17.0-64-bit.zip";


		private readonly ICmd2 cmd;

		private string gitCmdPath = null;


		public GitEnvironmentService(ICmd2 cmd)
		{
			this.cmd = cmd;
		}


		private static string GitFolderPath => Path.Combine(ProgramInfo.DataFolderPath, "Git", GitVersion);


		private static string GitFolderExePath(string gitFolderPath) =>
			Path.Combine(gitFolderPath, "cmd", "git.exe");


		public string GetGitCmdPath()
		{
			string gitFolderPath = GitFolderPath;
			string embeddedGitPath = GitFolderExePath(gitFolderPath);

			string gitPath = embeddedGitPath;

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

				Log.Info($"Using git: {gitFullPath}, Version: {gitVersion}");
				Track.Info($"Using git: {gitFullPath}, Version: {gitVersion}");
				if (gitPath != embeddedGitPath)
				{
					Track.Info($"{gitPath} != {embeddedGitPath}, start download gi in background");
					InstallGitAsync(s => { }).RunInBackground();
				}
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
				Track.Warn($"Expected git cmd path not found: {gitPath}");
				return null;
			}

			return gitPath;
		}


		public string TryGetGitVersion()
		{
			return TryGetCmdVersion(GetGitCmdPath());
		}


		private string TryGetCmdVersion(string cmdPath)
		{
			CmdResult2 result = cmd.Run(cmdPath, "version");

			return result.ExitCode == 0 && result.Output.StartsWithOic("git version ")
				? result.Output.Substring(12).Trim()
				: null;
		}


		public async Task<R> InstallGitAsync(Action<string> progress)
		{
			try
			{
				string gitFolderPath = GitFolderPath;
				string gitPath = GitFolderExePath(gitFolderPath);

				if (ExpectedGitExists(gitPath, GitVersion))
				{
					Track.Info($"Git: {gitPath}, version: {GitVersion} already exists");
					return R.Ok;
				}

				if (Directory.Exists(gitFolderPath))
				{
					Directory.Delete(gitFolderPath);
				}

				await InstallAsync(gitUri, gitFolderPath, progress);

				if (ExpectedGitExists(gitPath, GitVersion))
				{
					Track.Info($"Git: {gitPath}, version: {GitVersion} has been installed");
					return R.Ok;
				}

				Track.Error($"Git: Failed to install {gitUri}");
				return R.Error($"Failed to install {gitUri}");
			}
			catch (Exception e)
			{
				Log.Exception(e, $"Failed to install git {GitVersion}");
				return R.Error("Failed to install git", e);
			}
		}


		private static async Task InstallAsync(
			string uri, string gitFolderPath, Action<string> progress)
		{
			Track.Info($"Downloading git {GitVersion} from {uri} ...");
			Timing t = Timing.StartNew();
			string zipPath = ProgramInfo.GetTempFilePath() + $".git_{GitVersion}";

			using (var client = new HttpClientDownloadWithProgress(TimeSpan.FromSeconds(30)))
			{
				client.HttpClient.Timeout = TimeSpan.FromSeconds(60 * 5);
				client.HttpClient.DefaultRequestHeaders.Add("user-agent", "GitMind");

				client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) =>
				{
					progress($"Downloading git {(int)(progressPercentage ?? 0)}% ...");
					Track.Info($"Downloading git {progressPercentage}% (time: {t.Elapsed}) ...");
				};

				await client.StartDownloadAsync(uri, zipPath);
			}

			Track.Info($"Downloaded {uri} to {zipPath}");

			ZipFile.ExtractToDirectory(zipPath, gitFolderPath);
			Log.Info($"Unzipped {zipPath} to {gitFolderPath}");
			DeleteDownloadedFile(zipPath);
		}


		private static void DeleteDownloadedFile(string path)
		{
			try
			{
				File.Delete(path);
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to delete downloaded tmp file {path}, {e.Message}");
			}
		}


		private bool ExpectedGitExists(string gitPath, string expectedVersion)
		{
			if (File.Exists(gitPath))
			{
				string version = TryGetCmdVersion(gitPath);
				if (version == expectedVersion)
				{
					return true;
				}
			}

			return false;
		}
	}
}