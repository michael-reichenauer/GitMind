using System;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.ApplicationHandling
{
	internal class WorkingFolderService : IWorkingFolderService
	{
		private readonly ICommandLine commandLine;
		private readonly IGitInfoService gitInfoService;

		private string workingFolder;


		public WorkingFolderService(
			ICommandLine commandLine,
			IGitInfoService gitInfoService)
		{
			this.commandLine = commandLine;
			this.gitInfoService = gitInfoService;
		}

		public string WorkingFolder
		{
			get
			{
				if (workingFolder != null)
				{
					workingFolder = GetWorkingFolder();
				}

				return workingFolder;
			}
		}


		public void SetWorkingFolder(string path)
		{
			workingFolder = path;
		}



		// Must be able to handle:
		// * Starting app from start menu or pinned (no parameters and unknown current dir)
		// * Starting on command line in some dir (no parameters but known dir)
		// * Starting as right click on folder (parameter "/d:<dir>"
		// * Starting on command line with some parameters (branch names)
		// * Starting with parameters "/test"
		private string GetWorkingFolder()
		{
			string workingFolder = null;

			if (commandLine.HasFolder)
			{
				// Call from e.g. Windows Explorer folder context menu
				workingFolder = commandLine.Folder;
			}

			if (workingFolder == null)
			{
				workingFolder = TryGetWorkingFolder();
			}

			Log.Debug($"Current working folder {workingFolder}");
			return workingFolder;
		}


		private string TryGetWorkingFolder()
		{
			R<string> path = GetWorkingFolderPath(Environment.CurrentDirectory);

			if (!path.HasValue)
			{
				string lastUsedFolder = Settings.Get<ProgramSettings>().LastUsedWorkingFolder;

				if (!string.IsNullOrWhiteSpace(lastUsedFolder))
				{
					path = GetWorkingFolderPath(lastUsedFolder);
				}
			}

			if (path.HasValue)
			{
				return path.Value;
			}

			return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		}


		public R<string> GetWorkingFolderPath(string path)
		{
			if (path == null)
			{
				return Error.From("No working folder");
			}


			return gitInfoService.GetCurrentRootPath(path)
				.OnError(e => Log.Debug($"Not a working folder {path}, {e}"));
		}

	}
}