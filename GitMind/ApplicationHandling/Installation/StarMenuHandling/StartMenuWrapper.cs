namespace GitMind.ApplicationHandling.Installation.StarMenuHandling
{
	public static class StartMenuWrapper
	{
		public static void CreateStartMenuShortCut(
			string shortcutLocation,
			string targetPath,
			string arguments,
			string iconPath,
			string description)
		{

			IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
			IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)
				shell.CreateShortcut(shortcutLocation);

			shortcut.TargetPath = targetPath;
			shortcut.Arguments = "";
			shortcut.IconLocation = iconPath;
			shortcut.Description = ProgramInfo.ProgramName;

			shortcut.Save();
		}
	}
}