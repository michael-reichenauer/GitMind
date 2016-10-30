using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Threading;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Utils;


namespace GitMind.ApplicationHandling.Installation.Private
{
	internal class LatestVersionService : ILatestVersionService
	{
		private static readonly TimeSpan FirstCheckTime = TimeSpan.FromSeconds(1);
		private static readonly TimeSpan CheckIntervall = TimeSpan.FromHours(3);

		private static readonly string latestUri =
			"https://api.github.com/repos/michael-reichenauer/GitMind/releases/latest";
		private static readonly string UserAgent = "GitMind";

		private readonly ICmd cmd = new Cmd();

		private DispatcherTimer checkTimer;


		public void StartCheckForLatestVersion()
		{
			checkTimer = new DispatcherTimer();
			checkTimer.Tick += CheckLatestVersionAsync;
			checkTimer.Interval = FirstCheckTime;
			checkTimer.Start();
		}


		public async Task<bool> StartLatestInstalledVersionAsync()
		{
			await Task.Yield();

			try
			{
				string installedPath = ProgramPaths.GetInstallFilePath();

				cmd.Start(installedPath, null);
				return true;
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Error($"Failed to install new version {e}");
			}

			return false;
		}


		private async void CheckLatestVersionAsync(object sender, EventArgs e)
		{
			checkTimer.Interval = CheckIntervall;

			if (await IsNewRemoteVersionAvailableAsync())
			{
				await InstallLatestVersionAsync();

				// The actual installation (copy of files) is done by another, allow some time for that
				await Task.Delay(TimeSpan.FromSeconds(5));
			}

			NotifyNewVersionIsAvailable();
		}



		private async Task<bool> IsNewRemoteVersionAvailableAsync()
		{
			Log.Debug($"Checking remote version of {latestUri} ...");
			Version remoteVersion = await GetLatestRemoteVersionAsync();
			Version currentVersion = ProgramPaths.GetRunningVersion();
			Version installedVersion = ProgramPaths.GetInstalledVersion();

			LogVersion(currentVersion, installedVersion, remoteVersion);
			return installedVersion < remoteVersion;
		}


		private async Task<bool> InstallLatestVersionAsync()
		{
			try
			{
				Log.Debug($"Downloading remote setup {latestUri} ...");

				LatestInfo latestInfo = GetCachedLatestVersionInfo();

				using (HttpClient httpClient = GetHttpClient())
				{
					string setupPath = await DownloadSetupAsync(httpClient, latestInfo);

					InstallDownloadedSetup(setupPath);
					return true;
				}				
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Error($"Failed to install new version {e}");
			}

			return false;
		}


		private static async Task<string> DownloadSetupAsync(HttpClient httpClient, LatestInfo latestInfo)
		{
			Asset setupFileInfo = latestInfo.assets.First(a => a.name == "GitMindSetup.exe");

			string downloadUrl = setupFileInfo.browser_download_url;
			Log.Debug($"Downloading {latestInfo.tag_name} from {downloadUrl} ...");

			byte[] remoteFileData = await httpClient.GetByteArrayAsync(downloadUrl);

			string setupPath = ProgramPaths.GetTempFilePath() + "." + setupFileInfo.name;
			File.WriteAllBytes(setupPath, remoteFileData);

			Log.Debug($"Downloaded {latestInfo.tag_name} to {setupPath}");
			return setupPath;
		}


		private void InstallDownloadedSetup(string setupPath)
		{
			cmd.Start(setupPath, "/install /silent");
		}


		private LatestInfo GetCachedLatestVersionInfo()
		{
			ProgramSettings programSettings = Settings.Get<ProgramSettings>();
			
			return Json.As<LatestInfo>(programSettings.LatestVersionInfo);
		}


		private async Task<Version> GetLatestRemoteVersionAsync()
		{
			R<LatestInfo> latestInfo = await GetLatestInfoAsync();

			if (latestInfo.IsFaulted)
			{
				return new Version(0, 0, 0, 0);
			}

			Version version = Version.Parse(latestInfo.Value.tag_name.Substring(1));
			Log.Debug($"Remote version: {version}");
			foreach (var asset in latestInfo.Value.assets)
			{
				Log.Debug($"Name: {asset.name}, Count: {asset.download_count}");
			}

			return version;
		}


		private async Task<R<LatestInfo>> GetLatestInfoAsync()
		{
			try
			{
				using (HttpClient httpClient = GetHttpClient())
				{
					// Try get cached information about latest remote version
					ProgramSettings programSettings = Settings.Get<ProgramSettings>();
					string eTag = programSettings.LatestVersionInfoETag;
					string latestVersionInfo = programSettings.LatestVersionInfo;

					if (!string.IsNullOrEmpty(eTag))
					{
						// There is cached information, lets use the ETag when checking to follow
						// GitHub Rate Limiting method.
						httpClient.DefaultRequestHeaders.IfNoneMatch.Clear();
						httpClient.DefaultRequestHeaders.IfNoneMatch.Add(new EntityTagHeaderValue(eTag));
					}

					HttpResponseMessage response = await httpClient.GetAsync(latestUri);

					eTag = response.Headers.ETag.Tag;

					string latestInfoText;
					if (response.StatusCode == HttpStatusCode.NotModified)
					{
						Log.Debug("Remote latest version info same as cached info");						
						latestInfoText = latestVersionInfo;
					}
					else
					{
						latestInfoText = await response.Content.ReadAsStringAsync();
						Log.Debug("New version info");

						CacheLatestVersionInfo(eTag, latestInfoText);
					}

					return Json.As<LatestInfo>(latestInfoText);
				}
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Warn($"Failed to download latest setup: {e}");
				return e;
			}
		}


		private static void CacheLatestVersionInfo(string eTag, string latestInfoText)
		{
			ProgramSettings programSettings;
			if (!string.IsNullOrEmpty(eTag))
			{
				// Cache the latest version info
				programSettings = Settings.Get<ProgramSettings>();
				programSettings.LatestVersionInfoETag = eTag;
				programSettings.LatestVersionInfo = latestInfoText;
				Settings.Set(programSettings);
			}
		}


		private static void LogVersion(Version current, Version installed, Version remote)
		{
			Log.Usage($"Version current: {current}, installed: {installed} remote: {remote}");
		}


		private void NotifyNewVersionIsAvailable()
		{
			App.Current.Window.IsNewVersionVisible = IsNewVersionInstalled();
		}


		private bool IsNewVersionInstalled()
		{
			Version currentVersion = ProgramPaths.GetRunningVersion();
			Version installedVersion = ProgramPaths.GetInstalledVersion();

			Log.Debug($"Current version: {currentVersion} installed version: {installedVersion}");
			return currentVersion < installedVersion;
		}


		private static HttpClient GetHttpClient()
		{
			HttpClient httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("user-agent", UserAgent);
			return httpClient;
		}


		// Type used when parsing latest version information json
		public class LatestInfo
		{
			public string tag_name;
			public Asset[] assets;
		}

		// Type used when parsing latest version information json
		internal class Asset
		{
			public string name;
			public int download_count;
			public string browser_download_url;
		}
	}
}