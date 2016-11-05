using System;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.ApplicationHandling.Private
{
	[SingleInstance]
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


		public bool IsValid { get; private set; }


		public event EventHandler OnChange;

		public string Path
		{
			get
			{
				if (workingFolder == null)
				{
					workingFolder = GetInitialWorkingFolder();
					StoreLasteUsedFolder();
				}

				return workingFolder;
			}
		}


		public bool TrySetPath(string path)
		{
			R<string> rootFolder = GetRootFolderPath(path);

			if (rootFolder.HasValue)
			{
				IsValid = rootFolder.HasValue;

				if (workingFolder != path)
				{
					workingFolder = rootFolder.HasValue ? rootFolder.Value : commandLine.Folder;
					StoreLasteUsedFolder();
					OnChange?.Invoke(this, EventArgs.Empty);
				}
			}

			return rootFolder.HasValue;
		}



		private void StoreLasteUsedFolder()
		{
			if (IsValid)
			{
				ProgramSettings settings = Settings.Get<ProgramSettings>();
				settings.LastUsedWorkingFolder = workingFolder;
				Settings.Set(settings);
			}
		}


		// Must be able to handle:
		// * Starting app from start menu or pinned (no parameters and unknown current dir)
		// * Starting on command line in some dir (no parameters but known dir)
		// * Starting as right click on folder (parameter "/d:<dir>"
		// * Starting on command line with some parameters (branch names)
		// * Starting with parameters "/test"
		private string GetInitialWorkingFolder()
		{
			R<string> rootFolder;
			if (commandLine.HasFolder)
			{
				// Call from e.g. Windows Explorer folder context menu
				rootFolder = GetRootFolderPath(commandLine.Folder);
				IsValid = rootFolder.HasValue;
				return rootFolder.HasValue ? rootFolder.Value : commandLine.Folder;
			}

			rootFolder = GetRootFolderPath(Environment.CurrentDirectory);
			if (!rootFolder.HasValue)
			{
				string lastUsedFolder = GetLastUsedWorkingFolder();
				if (!string.IsNullOrWhiteSpace(lastUsedFolder))
				{
					rootFolder = GetRootFolderPath(lastUsedFolder);
				}
			}

			IsValid = rootFolder.HasValue;
			if (rootFolder.HasValue)
			{
				return rootFolder.Value;
			}

			return GetMyDocumentsPath();
		}


		private static string GetMyDocumentsPath()
		{
			return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		}


		private static string GetLastUsedWorkingFolder()
		{
			return Settings.Get<ProgramSettings>().LastUsedWorkingFolder;
		}


		public R<string> GetRootFolderPath(string path)
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