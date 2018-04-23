using GitMind.Utils;


namespace GitMind.ApplicationHandling
{
	[SingleInstance]
	internal class WorkingFolderPath
	{
		private readonly IWorkingFolderService workingFolderService;
		private string path;

		public WorkingFolderPath(string path)
		{
			this.path = path;
		}


		public WorkingFolderPath(IWorkingFolderService workingFolderService)
		{
			this.workingFolderService = workingFolderService;
		}


		public string Path => workingFolderService?.Path ?? path;

		public void SetPath(string newPath) => path = newPath;

		public static implicit operator string(WorkingFolderPath folderPath) => folderPath.Path;

	}
}