using System.IO;


namespace GitMind.Settings
{
	internal static class ProgramSettings
	{
		public static void SetLatestUsedWorkingFolderPath(string workingFolderPath)
		{
			string settingPath = ProgramPaths.GetSettingPath();
			string parentPath = Path.GetDirectoryName(settingPath);

			Directory.CreateDirectory(parentPath);

			File.WriteAllText(settingPath, workingFolderPath);
		}


		public static string TryGetLatestUsedWorkingFolderPath()
		{
			string settingPath = ProgramPaths.GetSettingPath();

			if (File.Exists(settingPath))
			{
				return File.ReadAllText(settingPath);
			}

			return "";
		}


		public static void SetLatestVersionETag(string etag)
		{
			string path = ProgramPaths.GetLatestETagPath();
			string parentPath = Path.GetDirectoryName(path);

			Directory.CreateDirectory(parentPath);

			File.WriteAllText(path, etag);
		}


		public static string TryGetLatestVersionETag()
		{
			string path = ProgramPaths.GetLatestETagPath();

			if (File.Exists(path))
			{
				return File.ReadAllText(path);
			}

			return "";
		}

		public static void SetLatestVersionInfo(string info)
		{
			string path = ProgramPaths.GetLatestInfoPath();
			string parentPath = Path.GetDirectoryName(path);

			Directory.CreateDirectory(parentPath);

			File.WriteAllText(path, info);
		}


		public static string TryGetLatestVersionInfo()
		{
			string path = ProgramPaths.GetLatestInfoPath();

			if (File.Exists(path))
			{
				return File.ReadAllText(path);
			}

			return "";
		}
	}
}