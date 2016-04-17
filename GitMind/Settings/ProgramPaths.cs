using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;


namespace GitMind.Settings
{
	internal static class ProgramPaths
	{
		private static readonly string remoteSetupFilePath1 =
			@"D:\My Work\GitMind\GitMind\bin\Debug\GitMind.exe";

		private static readonly string remoteSetupFilePath2 =
			@"\\storage03\n_axis_releases_sa\GitMind\GitMindSetup.exe";

		public static readonly string ProductGuid = "0000278d-5c40-4973-aad9-1c33196fd1a2";

		public static readonly string ProgramName = "GitMind";
		public static readonly string ProgramFileName = ProgramName + ".exe";
		private static readonly string ProgramShortcutFileName = ProgramName + ".lnk";
		private static readonly string SettingsFileName = "settings";


		public static string RemoteSetupPath
		{
			get
			{
				if (File.Exists(remoteSetupFilePath1))
				{
					return remoteSetupFilePath1;
				}

				return remoteSetupFilePath2;

			}
		}


		public static string GetSettingPath()
		{
			string programDataFolderPath = GetProgramDataFolderPath();
			return Path.Combine(programDataFolderPath, SettingsFileName);
		}


		public static string GetStartMenuShortcutPath()
		{
			string commonStartMenuPath = Environment.GetFolderPath(
				 Environment.SpecialFolder.StartMenu);
			string startMenuPath = Path.Combine(commonStartMenuPath, "Programs");

			return Path.Combine(startMenuPath, ProgramShortcutFileName);
		}


		public static string GetProgramFolderPath()
		{
			string programFolderPath = Environment.GetFolderPath(
				Environment.SpecialFolder.CommonApplicationData);

			return Path.Combine(programFolderPath, ProgramName);
		}


		public static string GetCurrentInstancePath()
		{
			return Assembly.GetEntryAssembly().Location;
		}


		public static string GetProgramDataFolderPath()
		{
			string programDataFolderPath = Environment.GetFolderPath(
				Environment.SpecialFolder.CommonApplicationData);

			return Path.Combine(programDataFolderPath, ProgramName);
		}


		public static string GetInstallFilePath()
		{
			string programFilesFolderPath = GetProgramFolderPath();
			return Path.Combine(programFilesFolderPath, ProgramFileName);
		}


		public static DateTime BuildTime()
		{
			string filePath = Assembly.GetEntryAssembly().Location;

			const int c_PeHeaderOffset = 60;
			const int c_LinkerTimestampOffset = 8;

			var buffer = new byte[2048];

			using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
				stream.Read(buffer, 0, 2048);

			var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
			var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

			var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

			var tz = TimeZoneInfo.Local;
			var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

			return localTime;
		}


		public static Version GetCurrentVersion()
		{
			return GetVersion(Assembly.GetEntryAssembly().Location);
		}

		public static Version GetVersion(string path)
		{
			if (!File.Exists(path))
			{
				Log.Debug($"path {path} does not exists");
				return new Version(0, 0, 0, 0);
			}

			try
			{
				FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(path);
				string versionText = fvi.ProductVersion;
				Version version = Version.Parse(versionText);

				return version;
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Warn($"Failed to get version from {path}, {e}");
				return new Version(0, 0, 0, 0);
			}
		}

		public static Result<string> GetWorkingFolderPath(string path)
		{
			IGitService gitService = new GitService();

			Environment.CurrentDirectory = path;

			return gitService.GetCurrentRootPath(null)
				.OnError(e => Log.Warn($"Not a working folder {path}, {e}"));
		}
	}
}