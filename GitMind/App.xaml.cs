using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using GitMind.ApplicationHandling;
using GitMind.ApplicationHandling.Installation;
using GitMind.Common.MessageDialogs;
using GitMind.MainWindowViews;
using GitMind.Utils;


namespace GitMind
{
	/// <summary>
	/// Interaction logic for App.xaml.
	/// </summary>
	public partial class App : Application
	{
		private readonly ICommandLine commandLine;
		private readonly Lazy<IApplicationService> applicationService;
		private readonly IInstaller installer;
		private readonly Lazy<MainWindow> mainWindow;


		public App(
			ICommandLine commandLine,
			Lazy<IApplicationService> applicationService,
			IInstaller installer,
			Lazy<MainWindow> mainWindow)
		{
			this.commandLine = commandLine;
			this.applicationService = applicationService;
			this.installer = installer;
			this.mainWindow = mainWindow;
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
				// A installation or uninstallation was triggered, lets end this instance
				Application.Current.Shutdown(0);
				return;
			}


			if (applicationService.Value.IsCommands())
			{
				// Command line contains some command like diff 
				// which will be handled and then this instance can end.
				HandleCommand();
				Application.Current.Shutdown(0);
				return;
			}

			if (applicationService.Value.IsActivatedOtherInstance())
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
			applicationService.Value.HandleCommands();
		}


		private void Start()
		{
			applicationService.Value.SetIsStarted();

			MainWindow = mainWindow.Value;
			MainWindow.Show();

			applicationService.Value.Start();
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
