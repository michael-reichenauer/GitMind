﻿using System;
using GitMind.Utils;


namespace GitMind.ApplicationHandling
{
	[SingleInstance]
	internal class WorkingFolder
	{
		private readonly IWorkingFolderService workingFolderService;


		public WorkingFolder(IWorkingFolderService workingFolderService)
		{
			this.workingFolderService = workingFolderService;
		}


		public event EventHandler OnChange
		{
			add { workingFolderService.OnChange += value; }
			remove { workingFolderService.OnChange -= value; }
		}
	

		public string Path => workingFolderService.WorkingFolder;

		public bool IsValid => workingFolderService.IsValid;

		public bool HasValue => Path != null;

		public string Name => HasValue ? System.IO.Path.GetFileName(Path) : null;

		public static implicit operator string(WorkingFolder workingFolder) => workingFolder.Path;


		public void SetPath(string path)
		{
			workingFolderService.SetWorkingFolder(path);
		}


		public bool TrySetPath(string path)
		{
			return workingFolderService.TrySetWorkingFolder(path);
		}
	}
}