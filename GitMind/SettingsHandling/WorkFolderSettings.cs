using System.Collections.Generic;
using GitMind.Git;


namespace GitMind.SettingsHandling
{
	public class WorkFolderSettings
	{
		public double Left { get; set; } = 100;
		public double Top { get; set; } = 100;
		public double Width { get; set; } = 800;
		public double Height { get; set; } = 695;
		public bool IsMaximized { get; set; } = false;
		public bool IsShowCommitDetails { get; set; } = false;
		public List<string> ShownBranches { get; set; } = new List<string>();
	}
}