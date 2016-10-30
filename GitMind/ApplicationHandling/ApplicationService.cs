using System;
using System.IO;
using System.Threading;
using GitMind.ApplicationHandling.Installation;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.GitModel;
using GitMind.MainWindowViews;
using GitMind.RepositoryViews;
using GitMind.Utils;


namespace GitMind.ApplicationHandling
{
	internal class ApplicationService
	{
		private readonly IDiffService diffService = new DiffService();
		private readonly ILatestVersionService latestVersionService = new LatestVersionService();
		private readonly ICommandLine commandLine;

		private readonly Lazy<string> lazyWorkingFolder;

		// This mutex is used by the installer (or uninstaller) to determine if instances are running
		private static Mutex applicationMutex;


		public ApplicationService(ICommandLine commandLine)
		{
			this.commandLine = commandLine;

			lazyWorkingFolder = new Lazy<string>(GetWorkingFolder);
		}


		public string WorkingFolder => lazyWorkingFolder.Value;

		public void SetIsStarted()
		{
			// This mutex is used by the installer (or uninstaller) to determine if instances are running
			applicationMutex = new Mutex(true, Installer.ProductGuid);
		}


		public void Start()
		{
			TryDeleteTempFiles();
			latestVersionService.StartCheckForLatestVersion();
		}


		public bool IsActivatedOtherInstance(string workingFolder)
		{
			string id = MainWindowIpcService.GetId(workingFolder);
			using (IpcRemotingService ipcRemotingService = new IpcRemotingService())
			{
				if (!ipcRemotingService.TryCreateServer(id))
				{
					// Another GitMind instance for that working folder is already running, activate that.
					var args = Environment.GetCommandLineArgs();
					ipcRemotingService.CallService<MainWindowIpcService>(id, service => service.Activate(args));
					return true;
				}
			}

			return false;
		}


		public bool IsCommands()
		{
			return commandLine.IsShowDiff;
		}


		public void HandleCommands()
		{
			if (commandLine.IsShowDiff)
			{
				diffService.ShowDiff(Commit.UncommittedId, WorkingFolder);
			}
		}


		public void TryDeleteTempFiles()
		{
			try
			{
				string tempFolderPath = ProgramPaths.GetTempFolderPath();
				string searchPattern = $"{ProgramPaths.TempPrefix}*";
				string[] tempFiles = Directory.GetFiles(tempFolderPath, searchPattern);
				foreach (string tempFile in tempFiles)
				{
					try
					{
						Log.Debug($"Deleting temp file {tempFile}");
						File.Delete(tempFile);
					}
					catch (Exception e)
					{
						Log.Debug($"Failed to delete temp file {tempFile}, {e.Message}. Deleting at reboot");
					}
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to delete temp files {e}");
			}
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