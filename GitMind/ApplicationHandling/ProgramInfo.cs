using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using GitMind.Utils;


namespace GitMind.ApplicationHandling
{
	internal static class ProgramInfo
	{
		public static readonly string ProgramName = "GitMind";
		public static readonly string ProgramFileName = ProgramName + ".exe";
		public static readonly string VersionFileName = ProgramName + ".Version.txt";
		public static readonly string TempPrefix = "_tmp_";


		public static string Version
		{
			get
			{

				Assembly assembly = Assembly.GetExecutingAssembly();
				FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
				return fvi.FileVersion;
			}
		}


		public static string ArgsText
		{
			get
			{
				string[] args = Environment.GetCommandLineArgs();
				string argsText = string.Join("','", args);
				return argsText;
			}
		}


		public static string DataFolderPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
			ProgramName);



		public static string GetTempFilePath()
		{
			string tempName = $"{TempPrefix}{Guid.NewGuid()}";
			string programDataFolderPath = GetProgramDataFolderPath();
			return Path.Combine(programDataFolderPath, tempName);
		}


		public static string GetTempFolderPath()
		{
			return GetProgramDataFolderPath();
		}


		public static string GetProgramFolderPath()
		{
			string programFolderPath = Environment.GetFolderPath(
				Environment.SpecialFolder.CommonApplicationData);

			return Path.Combine(programFolderPath, ProgramName);
		}


		public static string GetCurrentInstancePath()
		{
			return Assembly.GetEntryAssembly()?.Location;
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

		public static string GetVersionFilePath()
		{
			string programFilesFolderPath = GetProgramFolderPath();
			return Path.Combine(programFilesFolderPath, VersionFileName);
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


		public static Version GetRunningVersion()
		{
			AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
			return assemblyName.Version;
		}


		public static Version GetInstalledVersion()
		{
			try
			{
				if (File.Exists(GetVersionFilePath()))
				{
					string versionText = File.ReadAllText(GetVersionFilePath());
					return System.Version.Parse(versionText);
				}
				else
				{
					// This method does not always work running in stances has been moved.
					string installFilePath = GetInstallFilePath();
					if (!File.Exists(installFilePath))
					{
						return new Version(0, 0, 0, 0);
					}

					return GetVersion(installFilePath);
				}
			}
			catch (Exception)
			{
				return new Version(0, 0, 0, 0);
			}
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
				Version version = System.Version.Parse(versionText);

				return version;
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Exception(e, $"Failed to get version from {path}");
				return new Version(0, 0, 0, 0);
			}
		}
	}
}