namespace GitMind.ApplicationHandling.SettingsHandling
{
	public class DiffTool
	{
		public string Command { get; set; } = "C:\\Program Files\\Perforce\\p4merge.exe";

		public string Arguments { get; set; } = "%theirs %mine";
	}

	public class MergeTool
	{
		public string Command { get; set; } = "C:\\Program Files\\Perforce\\p4merge.exe";

		public string Arguments { get; set; } = "%base %theirs %mine %merged";
	}


	public class Options
	{
		public DiffTool DiffTool { get; set; } = new DiffTool();

		public MergeTool MergeTool { get; set; } = new MergeTool();
	}
}