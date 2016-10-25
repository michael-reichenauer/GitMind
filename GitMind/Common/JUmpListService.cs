using System.Windows;
using System.Windows.Shell;
using GitMind.Settings;


namespace GitMind.Common
{
	public class JumpListService
	{
		private static readonly int MaxTitleLength = 25;


		public void Add(string workingFolder)
		{
			JumpList jumpList = JumpList.GetJumpList(Application.Current) ?? new JumpList();

			string title = workingFolder.Length < MaxTitleLength
				? workingFolder
				: "..." + workingFolder.Substring(workingFolder.Length - MaxTitleLength);

			JumpTask jumpTask = new JumpTask();
			jumpTask.Title = title;
			jumpTask.ApplicationPath = ProgramPaths.GetInstallFilePath();
			jumpTask.Arguments = $"/d:\"{workingFolder}\"";
			jumpTask.IconResourcePath = ProgramPaths.GetInstallFilePath();
			jumpTask.Description = workingFolder;

			jumpList.ShowRecentCategory = true;

			JumpList.AddToRecentCategory(jumpTask);
			JumpList.SetJumpList(Application.Current, jumpList);
		}
	}
}