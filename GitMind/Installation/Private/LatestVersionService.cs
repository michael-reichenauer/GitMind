using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using GitMind.Settings;
using GitMind.Utils;


namespace GitMind.Installation.Private
{
	internal class LatestVersionService : ILatestVersionService
	{
		private static readonly string latestUri =
			"https://api.github.com/repos/michael-reichenauer/GitMind/releases/latest";
		private static readonly string UserAgent = "GitMind";

		private readonly JsonSerializerX serializer = new JsonSerializerX();
		private readonly ICmd cmd = new Cmd();


		public bool IsNewVersionInstalled()
		{
			Version currentVersion = ProgramPaths.GetCurrentVersion();
			Version installedVersion = ProgramPaths.GetInstalledVersion();

			Log.Debug($"Current version: {currentVersion} installed version: {installedVersion}");
			return currentVersion < installedVersion;
		}


		public async Task<bool> IsNewVersionAvailableAsync()
		{
			Log.Debug($"Checking remote version of {latestUri} ...");
			Version remoteVersion = await GetLatestRemoteVersionAsync();
			Version currentVersion = ProgramPaths.GetCurrentVersion();
			Version installedVersion = ProgramPaths.GetInstalledVersion();
			LogVersion(currentVersion, installedVersion, remoteVersion);

			return installedVersion < remoteVersion;
		}


		private static void LogVersion(Version current, Version installed, Version remote)
		{
			Log.Info($"Version current: {current}, installed: {installed} remote: {remote}");
		}


		private async Task<Version> GetLatestRemoteVersionAsync()
		{
			R<LatestInfo> latestInfo = await GetLatestInfoAsync();

			if (latestInfo.IsFaulted) return new Version(0, 0, 0, 0);

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
					string latestInfoText = await httpClient.GetStringAsync(latestUri);
				
					return serializer.Deserialize<LatestInfo>(latestInfoText);
				}
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Warn($"Failed to download latest setup: {e}");
				return e;
			}
		}


		public async Task<bool> InstallLatestVersionAsync()
		{
			try
			{
				Log.Debug($"Downloading remote setup {latestUri} ...");

				R<LatestInfo> latestInfo = await GetLatestInfoAsync();
				if (latestInfo.IsFaulted) return false;

				if (latestInfo.Value.assets != null)
				{
					Asset setupInfo = latestInfo.Value.assets.First(a => a.name == "GitMindSetup.exe");

					using (HttpClient httpClient = GetHttpClient())
					{
						Log.Debug(
							$"Downloading {latestInfo.Value.tag_name} from {setupInfo.browser_download_url}");

						byte[] remoteFileData = await httpClient.GetByteArrayAsync(
							setupInfo.browser_download_url);

						string tempPath = ProgramPaths.GetTempFilePath() + "." + setupInfo.name;
						File.WriteAllBytes(tempPath, remoteFileData);

						Log.Debug($"Downloaded {latestInfo.Value.tag_name} to {tempPath}");

						cmd.Start(tempPath, "/install /silent");
						return true;
					}
				}
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Error($"Failed to install new version {e}");
			}

			return false;
		}

		public async Task<bool> RunLatestVersionAsync()
		{
			await Task.Yield();
			try
			{
				cmd.Start(ProgramPaths.GetInstallFilePath(), null);
				return true;			
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Error($"Failed to install new version {e}");
			}

			return false;
		}

		private static HttpClient GetHttpClient()
		{
			HttpClient httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("user-agent", UserAgent);
			return httpClient;
		}


		[DataContract]
		public class LatestInfo
		{
			[DataMember]
			public string tag_name;

			[DataMember]
			public Asset[] assets;
		}

		[DataContract]
		internal class Asset
		{
			[DataMember]
			public string name;

			[DataMember]
			public int download_count;

			[DataMember]
			public string browser_download_url;
		}
	}
}