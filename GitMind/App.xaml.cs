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
		private readonly ILatestVersionService latestVersionService = new LatestVersionService();

		private readonly Lazy<IDiffService> diffService = new Lazy<IDiffService>(() => new DiffService());
		private readonly IInstaller installer = new Installer();

		private static Mutex programMutex;
		private DispatcherTimer newVersionTimer;
		private MainWindow mainWindow;

		public ICommandLine CommandLine { get; private set; }

		public new static App Current => (App)Application.Current;


		[STAThread]
		public static void Main()
		{
			AssemblyResolver.Activate();

			string version = GetProgramVersion();
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			string argsText = string.Join("','", commandLineArgs);

			Log.Debug($"Start version: {version}, Args: '{argsText}'");

			App application = new App();
			application.StartProgram();
		}


		private static string GetProgramVersion()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
			string version = fvi.FileVersion;
			return version;
		}



		protected override void OnExit(ExitEventArgs e)
		{
			Log.Usage("Exit program");
			base.OnExit(e);
		}


		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			newVersionTimer = new DispatcherTimer();
			ToolTipService.ShowDurationProperty.OverrideMetadata(
				typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));

			mainWindow = new MainWindow();
			MainWindow = mainWindow;

			// Installation code message boxes must for some reason be run after
			// main window is created and set. Maybe better when we have a "real" installation dialog
			if (!IsStartProgram())
			{
				Application.Current.Shutdown(0);
				return;
			}

			if (CommandLine.IsShowDiff)
			{
				Task.Run(() => diffService.Value.ShowDiffAsync(
					Commit.UncommittedId, CommandLine.WorkingFolder).Wait())
				.Wait();
				Application.Current.Shutdown(0);
				return;
			}


			string id = MainWindowIpcService.GetId(CommandLine.WorkingFolder);
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

		
			string version = GetProgramVersion();
			Log.Usage($"Start version: {version}");


			programMutex = new Mutex(true, ProgramPaths.ProductGuid);

			// Must not use WorkingFolder before installation code
			mainWindow.WorkingFolder = CommandLine.WorkingFolder;
			mainWindow.BranchNames = CommandLine.BranchNames.Select(name => new BranchName(name)).ToList();
			MainWindow.Show();

			newVersionTimer.Tick += NewVersionCheckAsync;
			newVersionTimer.Interval = TimeSpan.FromSeconds(5);
			newVersionTimer.Start();
		}


		private void StartProgram()
		{
			Serializer.RegisterSerializedTypes();
			CommandLine = new CommandLine();
			ExceptionHandling.Init();
			WpfBindingTraceListener.Register();


			InitializeComponent();

			Run();
		}


		private bool IsStartProgram()
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

				// The actual installation (copy of files) is done by another, allow some time for that
				await Task.Delay(TimeSpan.FromSeconds(5));
			}

			mainWindow.IsNewVersionVisible = latestVersionService.IsNewVersionInstalled();

			newVersionTimer.Interval = TimeSpan.FromHours(3);
		}
	}
}
