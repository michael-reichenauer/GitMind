using System.Windows;
using System.Windows.Shell;
using GitMind.Settings;


namespace GitMind.Common
{
	public class JumpListService
	{
		public void Add(string workingFolder)
		{
			JumpList jumpList = JumpList.GetJumpList(Application.Current) ?? new JumpList();

			JumpTask jumpTask = new JumpTask();
			jumpTask.Title = workingFolder;
			jumpTask.ApplicationPath = ProgramPaths.GetInstallFilePath();
			jumpTask.Arguments = $"/d:\"{workingFolder}\"";
			jumpTask.IconResourcePath = ProgramPaths.GetInstallFilePath();
			jumpTask.Description = $"Open {workingFolder}";

			jumpList.ShowRecentCategory = true;

			JumpList.AddToRecentCategory(jumpTask);
			JumpList.SetJumpList(Application.Current, jumpList);
		}
	}
}