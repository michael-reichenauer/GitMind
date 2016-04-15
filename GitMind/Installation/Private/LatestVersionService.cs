using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using GitMind.Settings;
using GitMind.Utils;


namespace GitMind.Installation.Private
{
	internal class LatestVersionService : ILatestVersionService
	{
		private static readonly string latestUri =
			"https://github.com/michael-reichenauer/GitMind/raw/latest/Releases/";

		private static readonly string latestVersionUri = latestUri + "version.txt";
		private static readonly string latestSetupUri = latestUri + "GitMindSetup.exe";


		public async Task<bool> IsNewVersionAvailableAsync()
		{
			return await Task.Run(() =>
			{
				try
				{
					Log.Debug($"Checking remote version of {latestVersionUri} ...");
					Version remoteSetupFileVersion = GetLatestRemoteVersion();

					Version currentVersion = ProgramPaths.GetCurrentVersion();
					Log.Debug($"Current version: {currentVersion} remote version: {remoteSetupFileVersion}");

					return currentVersion < remoteSetupFileVersion;
				}
				catch (Exception e)
				{
					Log.Error($"Failed to check remote version {e}");
				}

				return false;
			});
		}


		[AcceptingExceptions(typeof(ArgumentNullException))]
		[AcceptingExceptions(typeof(WebException))]
		private static Version GetLatestRemoteVersion()
		{
			WebClient webClient = new WebClient();
			string version = webClient.DownloadString(latestVersionUri).Trim();

			return Version.Parse(version);
		}


		public Task<bool> InstallLatestVersionAsync()
		{
			return Task.Run(() =>
			{
				try
				{
					Log.Debug($"Downloading remote setup {latestSetupUri} ...");

					byte[] remoteFileData = GetLatestRemoteSetup();

					string tempPath = Path.Combine(Path.GetTempPath(), "GitMindSetup.exe");
					File.WriteAllBytes(tempPath, remoteFileData);

					ProcessStartInfo info = new ProcessStartInfo(tempPath);
					info.UseShellExecute = true;
					Process.Start(info);
					return true;

				}
				catch (Exception e)
				{
					Log.Error($"Failed to install new version {e}");
				}

				return false;
			});
		}


		private static byte[] GetLatestRemoteSetup()
		{
			WebClient webClient = new WebClient();
			return webClient.DownloadData(latestSetupUri);
		}
	}
}