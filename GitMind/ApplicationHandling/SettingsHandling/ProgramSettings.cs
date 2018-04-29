using System.Collections.Generic;


namespace GitMind.ApplicationHandling.SettingsHandling
{
	internal class ProgramSettings
	{
		public string LatestVersionInfoETag { get; set; } = "";
		public string LatestVersionInfo { get; set; } = "";
		public List<string> ResentWorkFolderPaths { get; set; } = new List<string>();
		public List<string> ResentCloneUriPaths { get; set; } = new List<string>();
	}
}