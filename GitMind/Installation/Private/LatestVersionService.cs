using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using GitMind.Settings;
using GitMind.Utils;


namespace GitMind.Installation.Private
{
	internal class LatestVersionService : ILatestVersionService
	{
		public async Task<bool> IsNewVersionAvailableAsync()
		{
			return await Task.Run(() =>
			{
				try
				{
					string currentPath = ProgramPaths.GetCurrentInstancePath();
					string remoteSetupPath = ProgramPaths.RemoteSetupPath;

					Log.Debug($"Check version of {remoteSetupPath}");

					if (currentPath != remoteSetupPath)
					{
						Version currentVersion = ProgramPaths.GetCurrentVersion();
						Version remoteSetupFileVersion = ProgramPaths.GetVersion(remoteSetupPath);

						Log.Debug($"Version {currentVersion}, {remoteSetupFileVersion} of {remoteSetupPath}");

						return currentVersion < remoteSetupFileVersion;
					}
				}
				catch (Exception e)
				{
					Log.Error($"Failed to check remote version {e}");
				}

				return false;
			});
		}


		public Task<bool> InstallLatestVersionAsync()
		{
			return Task.Run(() =>
			{
				try
				{
					string remoteSetupPath = ProgramPaths.RemoteSetupPath;

					if (File.Exists(remoteSetupPath))
					{
						string tempSuffix = Path.GetFileNameWithoutExtension(Path.GetTempFileName());
						string tempName = "GitMindSetup_" + tempSuffix + ".exe";

						string tempPath = Path.Combine(Path.GetTempPath(), tempName);
						File.Copy(remoteSetupPath, tempPath, true);

						ProcessStartInfo info = new ProcessStartInfo(tempPath);
						info.UseShellExecute = true;
						Process.Start(info);
						return true;
					}
				}
				catch (Exception e)
				{
					Log.Error($"Failed to install new version {e}");
				}

				return false;
			});
		}
	}
}