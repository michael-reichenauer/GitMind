using System.Collections.Generic;
using System.Windows;
using GitMind.Common.Brushes;


namespace GitMind.ApplicationHandling.SettingsHandling
{
	public class Options
	{
		public DiffTool DiffTool { get; set; } = new DiffTool();

		public MergeTool MergeTool { get; set; } = new MergeTool();

		public Themes Themes { get; set; } = new Themes();
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