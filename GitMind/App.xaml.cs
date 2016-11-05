using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using GitMind.ApplicationHandling;
using GitMind.ApplicationHandling.Installation;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Common.MessageDialogs;
using GitMind.GitModel;
using GitMind.MainWindowViews;
using GitMind.RepositoryViews;
using GitMind.Utils;


namespace GitMind
{
	/// <summary>
	/// Interaction logic for App.xaml.
	/// </summary>
	public partial class App : Application
	{
		private readonly ICommandLine commandLine;
		private readonly IDiffService diffService;
		private readonly IInstaller installer;
		private readonly Lazy<MainWindow> mainWindow;
		private readonly WorkingFolder workingFolder;


		// This mutex is used by the installer (and uninstaller) to determine if instances are running
		private static Mutex applicationMutex;


		internal App(
			ICommandLine commandLine,
			IDiffService diffService,
			IInstaller installer,
			Lazy<MainWindow> mainWindow,
			WorkingFolder workingFolder)
		{
			this.commandLine = commandLine;
			this.diffService = diffService;
			this.installer = installer;
			this.mainWindow = mainWindow;
			this.workingFolder = workingFolder;
		}


		protected override void OnExit(ExitEventArgs e)
		{
			Log.Usage("Exit program");
			base.OnExit(e);
		}


		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			if (IsInstallOrUninstall())
			{
				// An installation or uninstallation was triggered, lets end this instance
				Application.Current.Shutdown(0);
				return;
			}


			if (IsCommands())
			{
				// Command line contains some command like diff 
				// which will be handled and then this instance can end.
				HandleCommand();
				Application.Current.Shutdown(0);
				return;
			}

			if (IsActivatedOtherInstance())
			{
				// Another instance for this working folder is already running and it received the
				// command line from this instance, lets exit this instance, while other instance continuous
				Application.Current.Shutdown(0);
				return;
			}

			Log.Usage($"Start version: {GetProgramVersion()}");
			Start();
		}


		private bool IsInstallOrUninstall()
		{
			if (commandLine.IsInstall || commandLine.IsUninstall)
			{
				// Tis is an installation (Setup file or "/install" arg) or uninstallation (/uninstall arg)
				// Need some temp main window when only message boxes will be shown for commands
				MainWindow = CreateTempMainWindow();

				installer.InstallOrUninstall();

				return true;
			}

			return false;
		}


		private void HandleCommand()
		{
			// Need some main window when only message boxes will be shown for commands
			MainWindow = CreateTempMainWindow();

			// Commands like Install, Uninstall, Diff, can be handled immediately
			HandleCommands();
		}


		private void Start()
		{
			// This mutex is used by the installer (or uninstaller) to determine if instances are running
			applicationMutex = new Mutex(true, Installer.ProductGuid);

			MainWindow = mainWindow.Value;
			MainWindow.Show();

			TryDeleteTempFiles();
		}


		private bool IsCommands()
		{
			return commandLine.IsShowDiff;
		}


		private void HandleCommands()
		{
			if (commandLine.IsShowDiff)
			{
				diffService.ShowDiff(Commit.UncommittedId, workingFolder);
			}
		}

		private bool IsActivatedOtherInstance()
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


		private void TryDeleteTempFiles()
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


		private static MessageDialog CreateTempMainWindow()
		{
			// Window used as a temp main window, when handling commands (i.e. no "real" main windows)
			return new MessageDialog(null, "", "", MessageBoxButton.OK, MessageBoxImage.Information);
		}


		private static string GetProgramVersion()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
			return fvi.FileVersion;
		}
	}
}
