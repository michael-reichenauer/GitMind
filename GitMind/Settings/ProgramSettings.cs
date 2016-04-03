using System.IO;


namespace GitMind.Settings
{
	internal static class ProgramSettings
	{
		public static void SetLatestUsedWorkingFolderPath(string workingFolderPath)
		{
			string settingPath = ProgramPaths.GetSettingPath();

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
	}
}