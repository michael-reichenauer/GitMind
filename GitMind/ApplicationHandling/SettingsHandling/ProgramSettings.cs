using System.Collections.Generic;


namespace GitMind.ApplicationHandling.SettingsHandling
{
	internal class ProgramSettings
	{
		//public string LastUsedWorkingFolder { get; set; } = "";
		public string LatestVersionInfoETag { get; set; } = "";
		public string LatestVersionInfo { get; set; } = "";
		public List<string> ResentWorkFolderPaths { get; set; } = new List<string>();
	}
}