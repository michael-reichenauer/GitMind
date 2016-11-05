using System;


namespace GitMind.ApplicationHandling
{
	internal interface IWorkingFolderService
	{
		event EventHandler OnChange;
		string WorkingFolder { get; }
		bool IsValid { get; }

		void SetWorkingFolder(string path);
		bool TrySetWorkingFolder(string path);
	}
}