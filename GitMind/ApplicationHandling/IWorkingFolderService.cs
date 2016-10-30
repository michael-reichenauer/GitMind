namespace GitMind.ApplicationHandling
{
	internal interface IWorkingFolderService
	{
		string WorkingFolder { get; }

		void SetWorkingFolder(string path);
	}
}