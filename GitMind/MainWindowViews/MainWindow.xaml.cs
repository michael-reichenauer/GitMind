using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using GitMind.Common;
using GitMind.Installation;
using GitMind.Installation.Private;
using GitMind.RepositoryViews;
using GitMind.Settings;
using GitMind.Testing;
using GitMind.Utils;
using Application = System.Windows.Application;


namespace GitMind.MainWindowViews
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly IAssemblyResolver assemblyResolver = new AssemblyResolver();
		private readonly ILatestVersionService latestVersionService = new LatestVersionService();
		private readonly IInstaller installer = new Installer();
		private readonly ICommandLine commandLine = new CommandLine();
		private readonly IDiffService diffService = new DiffService();


		private static Mutex programMutex;
		private readonly DispatcherTimer autoRefreshTime = new DispatcherTimer();
		private readonly DispatcherTimer newVersionTime = new DispatcherTimer();

		private readonly MainWindowViewModel mainWindowViewModel;
		private DateTime ActivatedTime = DateTime.MaxValue;
		private readonly List<string> specifiedBranchNames = new List<string>();
		private string workingFolder = null;


		public MainWindow()
		{
			ExceptionHandling.Init();
			assemblyResolver.Activate();

			if (!IsStartProgram())
			{
				Application.Current.Shutdown(0);
				return;
			}

			InitializeComponent();

			// Make sure maximize window does not cover the task bar
			MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight - 8;

			ToolTipService.ShowDurationProperty.OverrideMetadata(
				typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));

			mainWindowViewModel = new MainWindowViewModel(diffService, latestVersionService, this);


			InitDataModel();

			DataContext = mainWindowViewModel;
			mainWindowViewModel.WorkingFolder = workingFolder;
			mainWindowViewModel.SpecifiedBranchNames = specifiedBranchNames;

			StartBackgroundTasks();

			Activate();
		}


		private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			await mainWindowViewModel.FirstLoadAsync();
			ActivatedTime = DateTime.Now;
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


		// Must be able to handle:
		// * Starting app from start menu or pinned (no parameters and unknown current dir)
		// * Starting on command line in some dir (no parameters but known dir)
		// * Starting as right click on folder (parameter "/d:<dir>"
		// * Starting on command line with some parameters (branch names)
		// * Starting with parameters "/test"
		public void InitDataModel()
		{
			programMutex = new Mutex(true, ProgramPaths.ProductGuid);

			string[] args = Environment.GetCommandLineArgs();

			if (args.Length == 2 && args[1].StartsWith("/d:"))
			{
				// Call from e.g. Windows Explorer folder context menu
				workingFolder = args[1].Substring(3);
			}
			else if (args.Length == 2 && args[1] == "/test" && Directory.Exists(TestRepo.Path))
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

			if (workingFolder == null)
			{
				workingFolder = TryGetWorkingFolder();
			}

			Log.Debug($"Current working folder {workingFolder}");
		}


		private void StartBackgroundTasks()
		{
			newVersionTime.Tick += NewVersionAsync;
			newVersionTime.Interval = TimeSpan.FromSeconds(5);
			newVersionTime.Start();

			autoRefreshTime.Tick += AutoRefresh;
			autoRefreshTime.Interval = TimeSpan.FromMinutes(1);
			autoRefreshTime.Start();
		}


		private void AutoRefresh(object sender, EventArgs e)
		{
			try
			{
				mainWindowViewModel.AutoRefreshAsync().RunInBackground();
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


		protected override void OnActivated(EventArgs e)
		{
			if (ActivatedTime < DateTime.MaxValue && DateTime.Now - ActivatedTime > TimeSpan.FromSeconds(10))
			{
				Log.Debug("Refreshing after activation");
				mainWindowViewModel.ActivateRefreshAsync().RunInBackground();
				ActivatedTime = DateTime.Now;
			}
		}


		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			mainWindowViewModel.WindowWith = (int)sizeInfo.NewSize.Width;
		}


		private string TryGetWorkingFolder()
		{
			R<string> path = ProgramPaths.GetWorkingFolderPath(Environment.CurrentDirectory);
 
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
 
			return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
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
	}
}

