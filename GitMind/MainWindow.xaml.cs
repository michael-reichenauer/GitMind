using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using GitMind.CommitsHistory;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.GitModel;
using GitMind.GitModel.Private;
using GitMind.Installation;
using GitMind.Installation.Private;
using GitMind.Settings;
using GitMind.Utils;
using GitMind.Utils.UI;
using Application = System.Windows.Application;


namespace GitMind
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		//private readonly OldHistoryViewModel historyViewModel;
		private readonly RepositoryViewModel repositoryViewModel;
		private readonly IRepositoryService repositoryService = new RepositoryService();
		private readonly IStatusRefreshService refreshService;
		private readonly ILatestVersionService latestVersionService = new LatestVersionService();
		private readonly IInstaller installer = new Installer();
		private readonly ICommandLine commandLine = new CommandLine();
		private readonly IDiffService diffService = new DiffService();
		private readonly IGitService gitService = new GitService();

		private static Mutex programMutex;
		private readonly DispatcherTimer autoRefreshTime = new DispatcherTimer();
		private readonly DispatcherTimer newVersionTime = new DispatcherTimer();

		//private ZoomableCanvas canvas;
		private readonly MainWindowViewModel mainWindowViewModel;
		private DateTime LoadedTime = DateTime.MaxValue;
		private readonly List<string> specifiedBranchNames = new List<string>();
		private string workingFolder = null;

		public MainWindow()
		{
			AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;

			ExceptionHandling.Init();

			if (!IsStartProgram())
			{
				Application.Current.Shutdown(0);
				return;
			}

			InitializeComponent();
			//WindowChrome.SetWindowChrome(this, new WindowChrome());
			autoRefreshTime.Tick += FetchAndRefreshAsync;

			ToolTipService.ShowDurationProperty.OverrideMetadata(
				typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));

			repositoryViewModel = new RepositoryViewModel(
				new Lazy<BusyIndicator>(() => mainWindowViewModel.Busy));

			mainWindowViewModel = new MainWindowViewModel(
				repositoryViewModel,
				repositoryService,
				diffService,
				latestVersionService,
				this,
				() => RefreshAsync(true));

			refreshService = new StatusRefreshService(mainWindowViewModel);

			if (!InitDataModel())
			{
				Application.Current.Shutdown(0);
			}

			DataContext = mainWindowViewModel;
			mainWindowViewModel.WorkingFolder = workingFolder;
			mainWindowViewModel.SpecifiedBranchNames = specifiedBranchNames;

			StartBackgroundTasks();

			Activate();
		}


		static Assembly AssemblyResolve(object sender, ResolveEventArgs args)
		{
			try
			{
				Assembly executingAssembly = Assembly.GetExecutingAssembly();
				string name = executingAssembly.FullName.Split(',')[0];
				string resolveName = args.Name.Split(',')[0];
				string resourceName = $"{name}.Dependencies.{resolveName}.dll";
				Log.Debug($"Resolving {resolveName} from {resourceName} ...");

				if (resolveName == "LibGit2Sharp")
				{
					string gitName = "git2-785d8c4.dll";
					string directoryName = Path.GetDirectoryName(executingAssembly.Location);
					string targetPath = Path.Combine(directoryName, gitName);
					if (!File.Exists(targetPath))
					{
						string gitResourceName = $"{name}.Dependencies.{gitName}";
						Log.Debug($"Trying to extract {gitResourceName} and write {targetPath}");
						using (Stream stream = executingAssembly.GetManifestResourceStream(gitResourceName))
						{
							if (stream == null)
							{
								Log.Error($"Failed to read {gitResourceName}");
								throw new InvalidOperationException("Failed to extract dll" + gitResourceName);
							}

							long bytestreamMaxLength = stream.Length;
							byte[] buffer = new byte[bytestreamMaxLength];
							stream.Read(buffer, 0, (int)bytestreamMaxLength);
							File.WriteAllBytes(targetPath, buffer);
							Log.Debug($"Extracted {targetPath}");
						}
					}
				}

				// Load the assembly from the resources
				using (Stream stream = executingAssembly.GetManifestResourceStream(resourceName))
				{
					if (stream == null)
					{
						Log.Error($"Failed to load assembly {resolveName}");
						throw new InvalidOperationException("Failed to load assembly " + resolveName);
					}

					long bytestreamMaxLength = stream.Length;
					byte[] buffer = new byte[bytestreamMaxLength];
					stream.Read(buffer, 0, (int)bytestreamMaxLength);
					Log.Debug($"Resolved {resolveName}");
					return Assembly.Load(buffer);
				}
			}
			catch (Exception e)
			{
				Log.Error($"Failed to load, {e}");
				throw;
			}
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


		public bool InitDataModel()
		{
			programMutex = new Mutex(true, ProgramPaths.ProductGuid);

			string[] args = Environment.GetCommandLineArgs();

			if (args.Length == 2 && args[1].StartsWith("/d:"))
			{
				// Call from e.g. Windows Explorer folder context menu
				string currentDirectory = args[1].Substring(3);
				if (!string.IsNullOrWhiteSpace(currentDirectory))
				{
					workingFolder = currentDirectory;
				}

				args = new string[0];
			}

			if (args.Length == 2 && args[1] == "/test" && Directory.Exists(TestRepo.Path))
			{
				workingFolder = TestRepo.Path;
			}
			else if (args.Length > 1)
			{
				for (int i = 1; i < args.Length; i++)
				{
					specifiedBranchNames.Add(args[i]);
				}
			}

			workingFolder = TryGetWorkingFolder(workingFolder ?? Environment.CurrentDirectory);

			Log.Debug($"Current working folder {workingFolder}");
			return true;
		}


		private void StartBackgroundTasks()
		{
			newVersionTime.Tick += NewVersionAsync;
			newVersionTime.Interval = TimeSpan.FromSeconds(5);
			newVersionTime.Start();

			refreshService.Start();
			refreshService.UpdateStatusAsync(mainWindowViewModel.WorkingFolder).RunInBackground();
		}


		private async void FetchAndRefreshAsync(object sender, EventArgs e)
		{
			try
			{
				autoRefreshTime.Interval = TimeSpan.FromMinutes(10);

				//await Task.Yield();
				await RefreshAsync(true);
			}
			catch (Exception ex) when (ex.IsNotFatal())
			{
				Log.Error($"Failed to auto refresh {ex}");
			}
		}


		private async void NewVersionAsync(object sender, EventArgs e)
		{
			mainWindowViewModel.IsNewVersionVisible = await
				latestVersionService.IsNewVersionAvailableAsync();

			if (await latestVersionService.IsNewVersionAvailableAsync())
			{
				await latestVersionService.InstallLatestVersionAsync();
			}


			newVersionTime.Interval = TimeSpan.FromHours(3);
		}


		private string TryGetWorkingFolder(string workingFolder)
		{
			R<string> path = ProgramPaths.GetWorkingFolderPath(workingFolder);

			if (!path.HasValue)
			{
				string lastUsedFolder = ProgramSettings.TryGetLatestUsedWorkingFolderPath();

				if (!string.IsNullOrWhiteSpace(lastUsedFolder))
				{
					path = ProgramPaths.GetWorkingFolderPath(lastUsedFolder);
				}
			}

			if (path.HasValue)
			{
				return path.Value;
			}

			return null;
		}



		protected override void OnActivated(EventArgs e)
		{
			if (LoadedTime < DateTime.MaxValue && DateTime.Now - LoadedTime > TimeSpan.FromSeconds(10))
			{
				DispatcherTimer dispatcherTimer = new DispatcherTimer();
				dispatcherTimer.Tick += FetchAndRefreshAfterActivatedAsync;
				dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
				dispatcherTimer.Start();
			}
		}


		private async void FetchAndRefreshAfterActivatedAsync(object sender, EventArgs e)
		{
			try
			{
				DispatcherTimer dispatcherTimer = sender as DispatcherTimer;
				if (dispatcherTimer != null)
				{
					dispatcherTimer.Stop();
				}

				await RefreshAsync(true);
			}
			catch (Exception ex) when (ex.IsNotFatal())
			{
				Log.Warn($"Failed to refresh {ex}");
			}
		}

		private async Task RefreshAsync(bool isShift)
		{
			//await Task.Yield();
			//return;
			Task refreshTask = RefreshInternalAsync(isShift);
			mainWindowViewModel.Busy.Add(refreshTask);
			await refreshTask;
		}

		private async Task RefreshInternalAsync(bool isShift)
		{
			await refreshService.UpdateStatusAsync(repositoryViewModel.Repository.MRepository.WorkingFolder);

			await gitService.FetchAsync(repositoryViewModel.Repository.MRepository.WorkingFolder);

			Repository repository = await repositoryService.UpdateRepositoryAsync(
				repositoryViewModel.Repository);

			repositoryViewModel.Update(repository, repositoryViewModel.SpecifiedBranches);
		}

		//protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
		//{
		//	// Log.Debug($"Canvas offset {canvas.Offset}");

		//	if (e.ChangedButton == MouseButton.Left)
		//	{
		//		Point viewPoint = e.GetPosition(ItemsListBox);

		//		Point position = new Point(viewPoint.X + canvas.Offset.X, viewPoint.Y + canvas.Offset.Y);

		//		bool isControl = (Keyboard.Modifiers & ModifierKeys.Control) > 0;

		//		repositoryViewModel.Clicked(position, isControl);
		//	}

		//	base.OnPreviewMouseUp(e);
		//}




		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			// Log.Warn($"Size: {sizeInfo.NewSize.Width}");
			repositoryViewModel.Width = (int)sizeInfo.NewSize.Width;
		}



		protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
		{
			////if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
			////{
			////	// Adjust X in "e.Delta / X" to adjust zoom speed
			////	double x = Math.Pow(2, e.Delta / 10.0 / Mouse.MouseWheelDeltaForOneLine);
			////	double newScale = canvas.Scale * x;

			////	Log.Debug($"Scroll {x}, scale {canvas.Scale}, offset {canvas.Offset}");
			////	if (newScale < 0.5 || newScale > 10)
			////	{
			////		Log.Warn($"Zoom to large");
			////		e.Handled = true;
			////		return;
			////	}

			////	canvas.Scale = newScale;

			////	// Adjust the offset to make the point under the mouse stay still.
			////	Vector position = (Vector)e.GetPosition(ItemsListBox);
			////	canvas.Offset = (Point)((Vector)
			////		(canvas.Offset + position) * x - position);
			////	Log.Debug($"Scroll {x}, scale {canvas.Scale}, offset {canvas.Offset}");

			////	e.Handled = true;
			////}
		}


		private void HamburgerButton_OnClick(object sender, RoutedEventArgs e)
		{
			HamburgerContextMenu.PlacementTarget = this;
			HamburgerContextMenu.IsOpen = true;
		}


		private void MainWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			// Implement move/drag window in the title bar
			if (e.ChangedButton == MouseButton.Left)
			{
				DragMove();
			}
		}




		private void MyGridSplitter_DragStarted(object sender, DragStartedEventArgs e)
		{

		}


		private void MyGridSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
		{

		}


		private void MyGridSplitter_DragDelta(object sender, DragDeltaEventArgs e)
		{

		}
	}
}

