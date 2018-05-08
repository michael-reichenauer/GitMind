using System.IO;
using System.Windows;
using System.Windows.Shell;
using GitMind.ApplicationHandling;
using GitMind.ApplicationHandling.SettingsHandling;


namespace GitMind.Common
{
	public class JumpListService
	{
		private static readonly int MaxTitleLength = 25;


		public void Add(string workingFolder)
		{
			JumpList jumpList = JumpList.GetJumpList(Application.Current) ?? new JumpList();

			string folderName = Path.GetFileName(workingFolder) ?? workingFolder;

			string title = folderName.Length < MaxTitleLength
				? folderName
				: folderName.Substring(0, MaxTitleLength) + "...";

			JumpTask jumpTask = new JumpTask();
			jumpTask.Title = title;
			jumpTask.ApplicationPath = ProgramInfo.GetInstallFilePath();
			jumpTask.Arguments = $"/d:\"{workingFolder}\"";
			jumpTask.IconResourcePath = ProgramInfo.GetInstallFilePath();
			jumpTask.Description = workingFolder;

			jumpList.ShowRecentCategory = true;

			JumpList.AddToRecentCategory(jumpTask);
			JumpList.SetJumpList(Application.Current, jumpList);
		}
	}
}