﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using GitMind.ApplicationHandling;
using GitMind.ApplicationHandling.Installation;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ThemeHandling;
using GitMind.Common.Tracking;
using GitMind.Features.Diffing;
using GitMind.GitModel;
using GitMind.MainWindowViews;
using GitMind.Utils;
using GitMind.Utils.Ipc;


namespace GitMind
{
	/// <summary>
	/// Interaction logic for App.xaml.
	/// </summary>
	public partial class App : Application
	{
		private readonly ICommandLine commandLine;
		private readonly IDiffService diffService;
		private readonly IThemeService themeService;
		private readonly IInstaller installer;
		private readonly Lazy<MainWindow> mainWindow;
		private readonly WorkingFolder workingFolder;


		// This mutex is used by the installer (and uninstaller) to determine if instances are running
		private static Mutex applicationMutex;


		internal App(
			ICommandLine commandLine,
			IDiffService diffService,
			IThemeService themeService,
			IInstaller installer,
			Lazy<MainWindow> mainWindow,
			WorkingFolder workingFolder)
		{
			this.commandLine = commandLine;
			this.diffService = diffService;
			this.themeService = themeService;
			this.installer = installer;
			this.mainWindow = mainWindow;
			this.workingFolder = workingFolder;
		}


		protected override void OnExit(ExitEventArgs e)
		{
			Log.Usage("Exit program");
			Track.ExitProgram();
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

			if (commandLine.IsRunInstalled)
			{
				if (!WaitForOtherInstance())
				{
					// Another instance for this working folder is still running and did not close
					Application.Current.Shutdown(0);
					return;
				}
			}
			else
			{
				// Try once to activate existing instance for this working folder
				if (IsActivatedOtherInstance())
				{
					// Another instance for this working folder is already running and it received the
					// command line from this instance, lets exit this instance, while other instance continuous
					Application.Current.Shutdown(0);
					return;
				}
			}

			Log.Usage($"Start version: {GetProgramVersion()}");
			Track.StartProgram();
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

			themeService.SetThemeWpfColors();


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
				diffService.ShowDiff(CommitSha.Uncommitted);
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
						Log.Debug("Try activate other instance ...");
						var args = Environment.GetCommandLineArgs();
						ipcRemotingService.CallService<MainWindowIpcService>(id, service => service.Activate(args));
						Track.Event("ActivatedOtherInstance");
						return true;
					}
					else
					{
						Log.Debug("Continue with this instance...");
					}
				}
			}
			catch (Exception e)
			{
				Log.Exception(e, "Failed to activate other instance");
			}

			return false;
		}


		private bool WaitForOtherInstance()
		{
			Timing t = Timing.StartNew();
			while (t.Elapsed < TimeSpan.FromSeconds(20))
			{
				try
				{
					string id = MainWindowIpcService.GetId(workingFolder);
					using (IpcRemotingService ipcRemotingService = new IpcRemotingService())
					{
						if (ipcRemotingService.TryCreateServer(id))
						{
							Log.Debug("Other instance has closed");
							return true;
						}

					}
				}
				catch (Exception e)
				{
					Log.Exception(e, "Failed to check if other instance is running");
				}

				Thread.Sleep(100);
			}

			Log.Error("Failed to wait for other instance");
			return false;
		}


		private void TryDeleteTempFiles()
		{
			try
			{
				string tempFolderPath = ProgramInfo.GetTempFolderPath();
				string searchPattern = $"{ProgramInfo.TempPrefix}*";
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
				Log.Exception(e, "Failed to delete temp files");
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
