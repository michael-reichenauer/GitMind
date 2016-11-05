using System;


namespace GitMind.ApplicationHandling
{
	internal interface IWorkingFolderService
	{
		event EventHandler OnChange;
		string WorkingFolder { get; }
		bool IsValid { get; }

		bool TrySetWorkingFolder(string path);
	}
}