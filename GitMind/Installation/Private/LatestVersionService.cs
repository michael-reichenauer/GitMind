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
			Version currentedVersion = ProgramPaths.GetCurrentVersion();
			Version installedVersion = ProgramPaths.GetInstalledVersion();

			Log.Debug($"Current version: {currentedVersion} installed version: {installedVersion}");
			return currentedVersion < installedVersion;
		}


		public async Task<bool> IsNewVersionAvailableAsync()
		{
			Log.Debug($"Checking remote version of {latestUri} ...");
			Version remoteSetupFileVersion = await GetLatestRemoteVersionAsync();

			Version installedVersion = ProgramPaths.GetInstalledVersion();
			Log.Debug($"Installed version: {installedVersion} remote version: {remoteSetupFileVersion}");

			return installedVersion < remoteSetupFileVersion;
		}


		private async Task<Version> GetLatestRemoteVersionAsync()
		{
			R<LatestInfo> latestInfo = await GetLatestInfoAsync();

			if (latestInfo.IsFaulted) return new Version(0, 0, 0, 0);

			return Version.Parse(latestInfo.Value.tag_name.Substring(1));
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

						string tempPath = Path.Combine(Path.GetTempPath(), setupInfo.name);
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