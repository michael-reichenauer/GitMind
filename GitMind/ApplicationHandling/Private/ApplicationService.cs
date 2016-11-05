using System;
using System.IO;
using System.Threading;
using GitMind.ApplicationHandling.Installation;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.GitModel;
using GitMind.MainWindowViews;
using GitMind.RepositoryViews;
using GitMind.Utils;


namespace GitMind.ApplicationHandling.Private
{
	internal class ApplicationService : IApplicationService
	{
		private readonly IDiffService diffService;
		private readonly ILatestVersionService latestVersionService;
		private readonly WorkingFolder workingFolder;
		private readonly ICommandLine commandLine;
	

		// This mutex is used by the installer (or uninstaller) to determine if instances are running
		private static Mutex applicationMutex;


		public ApplicationService(
			ICommandLine commandLine, 
			IDiffService diffService,
			ILatestVersionService latestVersionService,
			WorkingFolder workingFolder)
		{
			this.commandLine = commandLine;
			this.diffService = diffService;
			this.latestVersionService = latestVersionService;
			this.workingFolder = workingFolder;
		}


	

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


		public bool IsActivatedOtherInstance()
		{
			try
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
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to activate other instance {e}");
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
				diffService.ShowDiff(Commit.UncommittedId, workingFolder);
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
	}
}