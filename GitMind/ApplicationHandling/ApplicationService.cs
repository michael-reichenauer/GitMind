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
		private readonly Lazy<IDiffService> diffService = new Lazy<IDiffService>(() => new DiffService());
		private readonly ICommandLine commandLine;
		private readonly WorkingFolderService workingFolderService;
		private readonly IInstaller installer = new Installer();

		// This mutex is used by the installer (or uninstaller) to determine if instances are running
		private static Mutex applicationMutex;

		public ApplicationService(
			ICommandLine commandLine,
			WorkingFolderService workingFolderService)
		{
			this.commandLine = commandLine;
			this.workingFolderService = workingFolderService;
		}


		public void SetIsStarted()
		{
			// This mutex is used by the installer (or uninstaller) to determine if instances are running
			applicationMutex = new Mutex(true, Installer.ProductGuid);
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
			return commandLine.IsShowDiff || commandLine.IsInstall || commandLine.IsUninstall;
		}

		public void HandleCommands()
		{


			if (commandLine.IsShowDiff)
			{
				diffService.Value.ShowDiff(Commit.UncommittedId, workingFolderService.WorkingFolder);
			}
			else
			{
				InstallOrUninstall();
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




		private bool InstallOrUninstall()
		{
			if (commandLine.IsInstall && !commandLine.IsSilent)
			{
				installer.InstallNormal();

				return false;
			}
			else if (commandLine.IsInstall && commandLine.IsSilent)
			{
				installer.InstallSilent();

				if (commandLine.IsRunInstalled)
				{
					installer.StartInstalled();
				}

				return false;
			}
			else if (commandLine.IsUninstall && !commandLine.IsSilent)
			{
				installer.UninstallNormal();

				return false;
			}
			else if (commandLine.IsUninstall && commandLine.IsSilent)
			{
				installer.UninstallSilent();

				return false;
			}

			TryDeleteTempFiles();

			return true;
		}
	}
}