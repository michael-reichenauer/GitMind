using System.Collections.Generic;
using System.Windows;
using GitMind.Common.ThemeHandling;


namespace GitMind.ApplicationHandling.SettingsHandling
{
	public class Options
	{
		public string comment => "Program options. Please restart GitMind after editing this file.";

		public DiffTool DiffTool { get; set; } = new DiffTool();

		public MergeTool MergeTool { get; set; } = new MergeTool();

		public ThemesOption Themes { get; set; } = new ThemesOption();
	}


	public class DiffTool
	{
		public string comment => "Specify external diff tool, with Arguments: %theirs %mine";

		public string Command { get; set; } = "C:\\Program Files\\Perforce\\p4merge.exe";

		public string Arguments { get; set; } = "%theirs %mine";
	}

	public class MergeTool
	{
		public string comment => "Specify external merge tool, with Arguments: %base %theirs %mine %merged";

		public string Command { get; set; } = "C:\\Program Files\\Perforce\\p4merge.exe";

		public string Arguments { get; set; } = "%base %theirs %mine %merged";
	}



}