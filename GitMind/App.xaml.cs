using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using GitMind.Common;
using GitMind.Installation;
using GitMind.Installation.Private;
using GitMind.MainWindowViews;
using GitMind.Settings;
using GitMind.Utils;
using GitMind.Utils.UI;
using Microsoft.Shell;


namespace GitMind
{
	/// <summary>
	/// Interaction logic for App.xaml.
	/// </summary>
	public partial class App : Application, ISingleInstanceApp
	{
		private readonly ILatestVersionService latestVersionService = new LatestVersionService();
		private readonly ICommandLine commandLine = new CommandLine();
		private readonly IInstaller installer = new Installer();

		private static Mutex programMutex;
		private DispatcherTimer newVersionTime;
		private MainWindow mainWindow;

		[STAThread]
		public static void Main()
		{		
			AssemblyResolver.Activate();
		
			App application = new App();
			application.StartProgram();


			//if (SingleInstance<App>.InitializeAsFirstInstance(ProgramPaths.ProductGuid))
			//{
			//	var application = new App();

			//	application.InitializeComponent();
			//	application.Run();

			//	// Allow single instance code to perform cleanup operations
			//	SingleInstance<App>.Cleanup();
			//}
			//else
			//{
			//	Log.Debug("Second instance is closing");
			//}
		}


		public bool SignalExternalCommandLineArgs(IList<string> args)
		{
			Log.Debug($"Got second instance argument ... '{string.Join(",", args)}'");
			Application.Current.MainWindow.Activate();
			return true;
		}


		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			newVersionTime = new DispatcherTimer();
			ToolTipService.ShowDurationProperty.OverrideMetadata(
				typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));

			mainWindow = new MainWindow(commandLine.WorkingFolder, commandLine.BranchNames);
			MainWindow = mainWindow;
			MainWindow.Show();

			newVersionTime.Tick += NewVersionCheckAsync;
			newVersionTime.Interval = TimeSpan.FromSeconds(5);
			newVersionTime.Start();
		}


		private void StartProgram()
		{
			ExceptionHandling.Init();
			WpfBindingTraceListener.Register();

			if (!IsStartProgram())
			{
				Application.Current.Shutdown(0);
				return;
			}

			programMutex = new Mutex(true, ProgramPaths.ProductGuid);

			InitializeComponent();

			Run();
		}


		private bool IsStartProgram()
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

			//string[] args = Environment.GetCommandLineArgs();
			//if (args.Length == 2 && args[1] == "/diff")
			//{
			//	diffService.ShowDiffAsync(null);
			//	return false;
			//}

			return true;
		}


		private async void NewVersionCheckAsync(object sender, EventArgs e)
		{
			if (await latestVersionService.IsNewVersionAvailableAsync())
			{
				await latestVersionService.InstallLatestVersionAsync();
			}

			mainWindow.IsNewVersionVisible = latestVersionService.IsNewVersionInstalled();

			newVersionTime.Interval = TimeSpan.FromHours(3);
		}
	}
}
