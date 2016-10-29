using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using GitMind.Common;
using GitMind.Common.MessageDialogs;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.Installation;
using GitMind.Installation.Private;
using GitMind.MainWindowViews;
using GitMind.RepositoryViews;
using GitMind.SettingsHandling;
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

		private readonly Lazy<IDiffService> diffService = new Lazy<IDiffService>(() => new DiffService());
		private readonly IInstaller installer = new Installer();
		private WorkingFolderService workingFolderService;

		private static Mutex programMutex;
		private DispatcherTimer newVersionTimer;
		private MainWindow mainWindow;


		public ICommandLine CommandLine { get; private set; }

		public new static App Current => (App)Application.Current;


		[STAThread]
		public static void Main()
		{
			Log.Debug(GetStartlineText());

			// Make sure that when assemblies that GitMind depends on are extracted whenever requested 
			AssemblyResolver.Activate();

			App app = new App();
			app.Start();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			Log.Usage("Exit program");
			base.OnExit(e);
		}


		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			OnStartup();
		}


		private void Start()
		{
			CommandLine = new CommandLine(Environment.GetCommandLineArgs());
			workingFolderService = new WorkingFolderService(CommandLine);
			ExceptionHandling.Init();
			WpfBindingTraceListener.Register();

			InitializeComponent();

			Run();
		}


		private void OnStartup()
		{
			if (IsCommands())
			{
				// Commands like Install, Uninstall, Diff, can be handled immediately
				HandleCommands();

				// Exit this instance after commands have been handled
				Application.Current.Shutdown(0);
				return;			
			}

			if (IsActivatedOtherInstance(workingFolderService.WorkingFolder))
			{
				// Another instance for this working folder is already running and it received the
				// command line from this instance, lets exit
				Application.Current.Shutdown(0);
				return;
			}
			
			StartNormalInstance();
		}


		private void StartNormalInstance()
		{
			Log.Usage($"Start version: {GetProgramVersion()}");
			programMutex = new Mutex(true, ProgramPaths.ProductGuid);

			SetShowToolTipsLongTime();

			installer.TryDeleteTempFiles();

			Serializer.RegisterCacheSerializedTypes();

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
			newVersionTimer.Interval = TimeSpan.FromSeconds(5);
			newVersionTimer.Start();
		}


		private static void SetShowToolTipsLongTime()
		{
			ToolTipService.ShowDurationProperty.OverrideMetadata(
				typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));
		}


		private bool IsActivatedOtherInstance(string workingFolder)
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


		private void HandleCommands()
		{
			// Need some main window when only message boxes will be shown for commands
			MainWindow = CreateTempMainWindow();

			if (CommandLine.IsShowDiff)
			{
				diffService.Value.ShowDiff(Commit.UncommittedId, workingFolderService.WorkingFolder);
			}
			else
			{
				InstallOrUninstall();
			}
		}


		private bool IsCommands()
		{
			return CommandLine.IsShowDiff || CommandLine.IsInstall || CommandLine.IsUninstall;
		}


		private bool InstallOrUninstall()
		{
			if (CommandLine.IsInstall && !CommandLine.IsSilent)
			{
				installer.InstallNormal();

				return false;
			}
			else if (CommandLine.IsInstall && CommandLine.IsSilent)
			{
				installer.InstallSilent();

				if (CommandLine.IsRunInstalled)
				{
					installer.StartInstalled();
				}

				return false;
			}
			else if (CommandLine.IsUninstall && !CommandLine.IsSilent)
			{
				installer.UninstallNormal();

				return false;
			}
			else if (CommandLine.IsUninstall && CommandLine.IsSilent)
			{
				installer.UninstallSilent();

				return false;
			}

			installer.TryDeleteTempFiles();

			return true;
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

			return $"Start version: {version}, Args: '{argsText}'";
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
