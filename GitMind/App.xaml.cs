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
using GitMind.Settings;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind
{
	/// <summary>
	/// Interaction logic for App.xaml.
	/// </summary>
	public partial class App : Application
	{
		private static readonly TimeSpan LatestCheckIntervall = TimeSpan.FromHours(12);

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
			CommandLine = new CommandLine();
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
				HandleCommands();

				Application.Current.Shutdown(0);
				return;			
			}

			newVersionTimer = new DispatcherTimer();
			ToolTipService.ShowDurationProperty.OverrideMetadata(
				typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));

			installer.TryDeleteTempFiles();


			mainWindow = new MainWindow();
			MainWindow = mainWindow;

			Serializer.RegisterSerializedTypes();
	

			string id = MainWindowIpcService.GetId(workingFolderService.WorkingFolder);
			using (IpcRemotingService ipcRemotingService = new IpcRemotingService())
			{
				if (!ipcRemotingService.TryCreateServer(id))
				{
					// Another GitMind instance for that working folder is already running, activate that.	
					ipcRemotingService.CallService<MainWindowIpcService>(id, service => service.Activate());

					Application.Current.Shutdown(0);
					return;
				}
			}


			string version = GetStartlineText();
			Log.Usage($"Start version: {version}");


			programMutex = new Mutex(true, ProgramPaths.ProductGuid);

			// Must not use WorkingFolder before installation code
			mainWindow.WorkingFolder = workingFolderService.WorkingFolder;
			mainWindow.BranchNames = CommandLine.BranchNames.Select(name => new BranchName(name)).ToList();
			MainWindow.Show();

			newVersionTimer.Tick += NewVersionCheckAsync;
			newVersionTimer.Interval = TimeSpan.FromSeconds(5);
			newVersionTimer.Start();
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
			if (await latestVersionService.IsNewVersionAvailableAsync())
			{
				await latestVersionService.InstallLatestVersionAsync();

				// The actual installation (copy of files) is done by another, allow some time for that
				await Task.Delay(TimeSpan.FromSeconds(5));
			}

			mainWindow.IsNewVersionVisible = latestVersionService.IsNewVersionInstalled();

			newVersionTimer.Interval = LatestCheckIntervall;
		}


		private static string GetStartlineText()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
			string version = fvi.FileVersion;

			string[] commandLineArgs = Environment.GetCommandLineArgs();
			string argsText = string.Join("','", commandLineArgs);

			return $"Start version: {version}, Args: '{argsText}'";
		}


		private static MessageDialog CreateTempMainWindow()
		{
			// Window used as a temp main window
			return new MessageDialog(null, "", "", MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}
}
