using System;
using System.IO;
using GitMind.ApplicationHandling.Installation;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.ApplicationHandling.Testing;
using GitMind.Utils;


namespace GitMind.ApplicationHandling
{
	internal class WorkingFolderService
	{
		private readonly ICommandLine commandLine;

		private readonly Lazy<string> lazyWorkingFolder;


		public WorkingFolderService(ICommandLine commandLine)
		{
			this.commandLine = commandLine;
			lazyWorkingFolder = new Lazy<string>(GetWorkingFolder);
		}

		public string WorkingFolder => lazyWorkingFolder.Value;


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
			else if (commandLine.IsTest && Directory.Exists(TestRepo.Path))
			{
				workingFolder = TestRepo.Path;
			}

			if (workingFolder == null)
			{
				workingFolder = TryGetWorkingFolder();
			}

			Log.Debug($"Current working folder {workingFolder}");
			return workingFolder;
		}


		private static string TryGetWorkingFolder()
		{
			R<string> path = ProgramPaths.GetWorkingFolderPath(Environment.CurrentDirectory);

			if (!path.HasValue)
			{
				
				string lastUsedFolder = Settings.Get<ProgramSettings>().LastUsedWorkingFolder;

				if (!string.IsNullOrWhiteSpace(lastUsedFolder))
				{
					path = ProgramPaths.GetWorkingFolderPath(lastUsedFolder);
				}
			}

			if (path.HasValue)
			{
				return path.Value;
			}

			return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		}
	}
}