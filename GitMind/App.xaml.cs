using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GitMind.ApplicationHandling;
using GitMind.ApplicationHandling.Installation;
using GitMind.ApplicationHandling.Installation.Private;
using GitMind.ApplicationHandling.SettingsHandling;
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
		private static readonly TimeSpan LatestCheckIntervall = TimeSpan.FromHours(3);

		private readonly ILatestVersionService latestVersionService = new LatestVersionService();

		private readonly IInstaller installer = new Installer();
		private readonly OtherInstanceService instanceService = new OtherInstanceService();
		private WorkingFolderService workingFolderService;
		private ApplicationService applicationService;

		private DispatcherTimer newVersionTimer;
		private MainWindow mainWindow;
		private static readonly TimeSpan FirstLastestVersionCheckTime = TimeSpan.FromSeconds(1);


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

			applicationService.TryDeleteTempFiles();

			ShowMainWindow();

			StartCheckForLatestVersion();
		}


		private void ShowMainWindow()
		{
			mainWindow = new MainWindow();
			MainWindow = mainWindow;

			mainWindow.WorkingFolder = workingFolderService.WorkingFolder;
			mainWindow.BranchNames = CommandLine.BranchNames.Select(name => new BranchName(name)).ToList();
			MainWindow.Show();
		}


		private void StartCheckForLatestVersion()
		{
			newVersionTimer = new DispatcherTimer();
			newVersionTimer.Tick += NewVersionCheckAsync;
			newVersionTimer.Interval = FirstLastestVersionCheckTime;
			newVersionTimer.Start();
		}


		private async void NewVersionCheckAsync(object sender, EventArgs e)
		{
			newVersionTimer.Interval = LatestCheckIntervall;

			if (await latestVersionService.IsNewVersionAvailableAsync())
			{
				await latestVersionService.InstallLatestVersionAsync();

				// The actual installation (copy of files) is done by another, allow some time for that
				await Task.Delay(TimeSpan.FromSeconds(5));
			}

			mainWindow.IsNewVersionVisible = latestVersionService.IsNewVersionInstalled();
		}



		private static string GetStartlineText()
		{
			string version = GetProgramVersion();

			string[] commandLineArgs = Environment.GetCommandLineArgs();
			string argsText = string.Join("','", commandLineArgs);

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
			// Window used as a temp main window
			return new MessageDialog(null, "", "", MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}
}
