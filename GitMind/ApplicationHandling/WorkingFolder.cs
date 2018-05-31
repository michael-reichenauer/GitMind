using System;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind.ApplicationHandling
{
	[SingleInstance]
	internal class WorkingFolder : IWorkingFolder
	{
		private readonly IWorkingFolderService workingFolderService;
		private string path;

		public WorkingFolder(IWorkingFolderService workingFolderService)
		{
			this.workingFolderService = workingFolderService;
		}


		public WorkingFolder(string path)
		{
			this.path = path;
		}


		public event EventHandler OnChange
		{
			add => workingFolderService.OnChange += value;
			remove => workingFolderService.OnChange -= value;
		}


		public string Path => workingFolderService?.Path ?? path;

		public bool IsValid => workingFolderService?.IsValid ?? true;

		public bool HasValue => Path != null;

		public string Name => HasValue ? System.IO.Path.GetFileName(Path) : null;

		public static implicit operator string(WorkingFolder workingFolder) => workingFolder.Path;


		public bool TrySetPath(string newPath)
		{
			if (workingFolderService != null)
			{
				return workingFolderService.TrySetPath(newPath);
			}
			else
			{
				path = newPath;
				return true;
			}
		}


		public override string ToString() => Path;
	}
}