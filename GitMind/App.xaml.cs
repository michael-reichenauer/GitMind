﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using GitMind.ApplicationHandling;
using GitMind.ApplicationHandling.Installation;
using GitMind.ApplicationHandling.Installation.Private;
using GitMind.Common;
using GitMind.Common.MessageDialogs;
using GitMind.Git;
using GitMind.MainWindowViews;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind
{
	/// <summary>
	/// Interaction logic for App.xaml.
	/// </summary>
	public partial class App : Application
	{
		private readonly ILatestVersionService latestVersionService = new LatestVersionService();
		private readonly OtherInstanceService instanceService = new OtherInstanceService();
		private WorkingFolderService workingFolderService;
		private ApplicationService applicationService;


		public MainWindow Window;

		public ICommandLine CommandLine { get; private set; }

		public new static App Current => (App)Application.Current;


		[STAThread]
		public static void Main()
		{
			Log.Debug(GetStartlineText());

			// Make sure that when assemblies that GitMind depends on are extracted whenever requested 
			AssemblyResolver.Activate();

			App application = new App();
			ExceptionHandling.Init();
			WpfBindingTraceListener.Register();
			application.InitializeComponent();
			application.Run();
		}


		protected override void OnExit(ExitEventArgs e)
		{
			Log.Usage("Exit program");
			base.OnExit(e);
		}


		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			CommandLine = new CommandLine(Environment.GetCommandLineArgs());
			workingFolderService = new WorkingFolderService(CommandLine);
			applicationService = new ApplicationService(CommandLine, workingFolderService);

			if (applicationService.IsCommands())
			{
				// Command line contains some command like install, diff, ..., which will be handled
				// Need some main window when only message boxes will be shown for commands
				MainWindow = CreateTempMainWindow();

				// Commands like Install, Uninstall, Diff, can be handled immediately
				applicationService.HandleCommands();

				// Exit this instance after commands have been handled
				Application.Current.Shutdown(0);
				return;
			}

			if (instanceService.IsActivatedOtherInstance(workingFolderService.WorkingFolder))
			{
				// Another instance for this working folder is already running and it received the
				// command line from this instance, lets exit this instance, while other instance continuous
				Application.Current.Shutdown(0);
				return;
			}

			Log.Usage($"Start version: {GetProgramVersion()}");
			Start();
		}


		private void Start()
		{
			applicationService.SetIsStarted();

			ShowMainWindow();

			applicationService.TryDeleteTempFiles();
			latestVersionService.StartCheckForLatestVersion();
		}


		private void ShowMainWindow()
		{
			Window = new MainWindow();
			MainWindow = Window;

			Window.WorkingFolder = workingFolderService.WorkingFolder;
			Window.BranchNames = CommandLine.BranchNames.Select(name => new BranchName(name)).ToList();
			MainWindow.Show();
		}


		private static string GetStartlineText()
		{
			string version = GetProgramVersion();

			string[] args = Environment.GetCommandLineArgs();
			string argsText = string.Join("','", args);

			return $"Start version: {version}, args: '{argsText}'";
		}


		private static string GetProgramVersion()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
			return fvi.FileVersion;
		}


		private static MessageDialog CreateTempMainWindow()
		{
			// Window used as a temp main window, when handling commands (i.e. no "real" main windows)
			return new MessageDialog(null, "", "", MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}
}
