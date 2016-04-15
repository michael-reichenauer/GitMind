using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using GitMind.CommitsHistory;
using GitMind.Installation;
using GitMind.Installation.Private;
using GitMind.Settings;
using GitMind.Utils;
using GitMind.VirtualCanvas;


namespace GitMind
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly HistoryViewModel historyViewModel;
		private readonly IStatusRefreshService refreshService;
		private readonly ILatestVersionService latestVersionService = new LatestVersionService();
		private readonly IInstaller installer = new Installer();
		private readonly ICommandLine commandLine = new CommandLine();
		private readonly IDiffService diffService = new DiffService();

		private static Mutex programMutex;
		private readonly DispatcherTimer autoRefreshTime = new DispatcherTimer();
		private readonly DispatcherTimer newVersionTime = new DispatcherTimer();

		private ZoomableCanvas canvas;
		private readonly MainWindowViewModel mainWindowViewModel;
		private DateTime LoadedTime = DateTime.MaxValue;


		public MainWindow()
		{
			ExceptionHandling.Init();

			if (!IsStartProgram())
			{
				Application.Current.Shutdown(0);
				return;
			}

			Application.Current.DispatcherUnhandledException += UnhandledExceptionHandler;

			InitializeComponent();

			ToolTipService.ShowDurationProperty.OverrideMetadata(
				typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));

			historyViewModel = new HistoryViewModel();

			mainWindowViewModel = new MainWindowViewModel(
				historyViewModel, diffService, latestVersionService, this);

			refreshService = new StatusRefreshService(mainWindowViewModel);

			InitDataModel();

			DataContext = mainWindowViewModel;

			ItemsListBox.ItemsSource = historyViewModel.ItemsSource;

			StartBackgroundTasks();

			Activate();
		}


		private bool IsStartProgram()
		{
			if (commandLine.IsNormalInstallation())
			{
				installer.InstallNormal();

				return false;
			}
			else if (commandLine.IsSilentInstallation())
			{
				installer.InstallSilent();

				return false;
			}
			else if (commandLine.IsNormalUninstallation())
			{
				installer.UninstallNormal();

				return false;
			}
			else if (commandLine.IsSilentUninstallation())
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


		public void InitDataModel()
		{
			programMutex = new Mutex(true, ProgramPaths.ProductGuid);

			string[] args = Environment.GetCommandLineArgs();

			if (args.Length == 2 && args[1].StartsWith("/d:"))
			{
				// Call from e.g. Windows Explorer folder context menu
				string currentDirectory = args[1].Substring(3);
				if (!string.IsNullOrWhiteSpace(currentDirectory))
				{
					Environment.CurrentDirectory = currentDirectory;
				}

				args = new string[0];
			}

			List<string> specifiedBranchNames = new List<string>();

			if (args.Length == 2 && args[1] == "/test")
			{
				Environment.CurrentDirectory = TestRepo.Path2;
			}
			else if (args.Length > 1)
			{
				for (int i = 1; i < args.Length; i++)
				{
					specifiedBranchNames.Add(args[i]);
				}
			}

			SetWorkingFolder();
			Log.Debug($"Current working folder {Environment.CurrentDirectory}");

			historyViewModel.SetBranches(specifiedBranchNames);
		}


		private void StartBackgroundTasks()
		{
			Log.Debug("Start version timer");
			newVersionTime.Tick += NewVersionAsync;
			newVersionTime.Interval = TimeSpan.FromSeconds(5);
			newVersionTime.Start();

			refreshService.Start();
			refreshService.UpdateStatusAsync().RunInBackground();
		}


		private async void FetchAndRefreshAsync(object sender, EventArgs e)
		{
			try
			{
				autoRefreshTime.Interval = TimeSpan.FromMinutes(10);

				await RefreshAsync(true);
			}
			catch (Exception ex)
			{
				Log.Error($"Failed to auto refresh {ex}");
			}
		}



		private async void NewVersionAsync(object sender, EventArgs e)
		{
			mainWindowViewModel.IsNewVersionVisible.Set(false);

			bool isNewVersion = await latestVersionService.IsNewVersionAvailableAsync();

			mainWindowViewModel.IsNewVersionVisible.Set(isNewVersion);

			newVersionTime.Interval = TimeSpan.FromMinutes(60);
		}


		private void SetWorkingFolder()
		{
			Result<string> workingFolder = ProgramPaths.GetWorkingFolderPath(
				Environment.CurrentDirectory);

			if (!workingFolder.HasValue)
			{
				string lastUsedFolder = ProgramSettings.TryGetLatestUsedWorkingFolderPath();

				if (!string.IsNullOrWhiteSpace(lastUsedFolder))
				{
					workingFolder = ProgramPaths.GetWorkingFolderPath(lastUsedFolder);
				}
			}

			workingFolder.OnValue(v => Environment.CurrentDirectory = v);
		}


		private async void ZoomableCanvas_Loaded(object sender, RoutedEventArgs e)
		{
			// Store the canvas in a local variable since x:Name doesn't work.
			canvas = (ZoomableCanvas)sender;

			mainWindowViewModel.WorkingFolder.Set(
				ProgramPaths.GetWorkingFolderPath(Environment.CurrentDirectory).Or(""));

			await historyViewModel.LoadAsync(this);
			LoadedTime = DateTime.Now;

			autoRefreshTime.Tick += FetchAndRefreshAsync;
			autoRefreshTime.Interval = TimeSpan.FromSeconds(2);
			autoRefreshTime.Start();
		}


		protected override void OnActivated(EventArgs e)
		{
			Log.Debug("On activated");
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
			catch (Exception ex)
			{
				Log.Warn($"Failed to refresh {ex}");
			}
		}



		private async Task RefreshAsync(bool isShift)
		{
			await refreshService.UpdateStatusAsync();
			await historyViewModel.RefreshAsync(isShift);
		}

		protected override async void OnPreviewMouseUp(MouseButtonEventArgs e)
		{
			// Log.Debug($"Canvas offset {canvas.Offset}");

			Point viewPoint = e.GetPosition(ItemsListBox);

			Point position = new Point(viewPoint.X + canvas.Offset.X, viewPoint.Y + canvas.Offset.Y);

			bool isControl = (Keyboard.Modifiers & ModifierKeys.Control) > 0;

			await historyViewModel.ClickedAsync(position, isControl);

			base.OnPreviewMouseUp(e);
		}


		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				Close();
			}
		}


		protected override async void OnKeyUp(KeyEventArgs e)
		{
			if (e.Key == Key.F5)
			{
				bool isShift = (Keyboard.Modifiers & ModifierKeys.Shift) > 0;
				Log.Debug("Refresh");
				await RefreshAsync(isShift);
			}

			base.OnKeyUp(e);
		}


		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			// Log.Warn($"Size: {sizeInfo.NewSize.Width}");
			historyViewModel.Width = sizeInfo.NewSize.Width;
		}


		protected override void OnPreviewMouseMove(MouseEventArgs e)
		{
			Point position = e.GetPosition(ItemsListBox);
			//Log.Debug($"Position {position}");
			if (e.LeftButton == MouseButtonState.Pressed
				&& position.Y < 0 && position.X < (canvas.ActualWidth - 320))
			{
				DragMove();
			}

			//Point position = e.GetPosition(ItemsListBox);
			//if (e.LeftButton == MouseButtonState.Pressed
			//		&& !(e.OriginalSource is Thumb)) // Don't block the scrollbars.
			//{
			//	Log.Debug($"Mouse {position}");
			//	CaptureMouse();
			//	canvas.Offset -= position - lastMousePosition;
			//	e.Handled = true;
			//}
			//else
			//{
			//	ReleaseMouseCapture();
			//}

			//lastMousePosition = position;
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

		private void UnhandledExceptionHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			Log.Error("Unhandled Error: " + e.Exception);

			MessageBox.Show(
				Application.Current.MainWindow,
				"Unhandled Error: " + e.Exception,
				"GitMind - Unhandled Exception",
				MessageBoxButton.OK,
				MessageBoxImage.Error);

			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
			else
			{
				Application.Current.Shutdown(-1);
			}

			e.Handled = true;
		}


		private void CloseButton_OnClick(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown(0);
		}


		private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}
	}
}

