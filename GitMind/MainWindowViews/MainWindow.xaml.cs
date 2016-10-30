using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using GitMind.ApplicationHandling;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.MainWindowViews
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly IWorkingFolderService workingFolderService;
		private readonly ICommandLine commandLine;
		private static readonly TimeSpan remoteCheckInterval = TimeSpan.FromMinutes(10);

		private readonly DispatcherTimer remoteCheckTimer = new DispatcherTimer();

		private readonly MainWindowViewModel viewModel;


		internal MainWindow(
			IWorkingFolderService workingFolderService,
			ICommandLine commandLine,
			Func<MainWindow, Action, Action, MainWindowViewModel> mainWindowViewModelProvider)
		{
			this.workingFolderService = workingFolderService;
			this.commandLine = commandLine;

			InitializeComponent();

			SetShowToolTipLonger();

			// Make sure maximize window does not cover the task bar
			MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight - 8;

			viewModel = mainWindowViewModelProvider(
				this,
				() => Search.SearchBox.Focus(),
				() => RepositoryView.ItemsListBox.Focus());
			DataContext = viewModel;

			Activate();

			RestoreWindowSettings(workingFolderService.WorkingFolder);
			SetBranchNames();
		}


		private void SetBranchNames()
		{
			IReadOnlyList<string> names = commandLine.BranchNames;

			if (!names.Any())
			{
				names = RestoreShownBranches();
			}

			List<BranchName> branchNames = names.Select(name => new BranchName(name)).ToList();
			viewModel.SpecifiedBranchNames = branchNames;
		}



		public bool IsNewVersionAvailable
		{
			set { viewModel.IsNewVersionVisible = value; }
		}


		private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			await viewModel.FirstLoadAsync();

			StartRemoteCheck();		
		}


		private void StartRemoteCheck()
		{
			remoteCheckTimer.Tick += RemoteCheck;
			remoteCheckTimer.Interval = remoteCheckInterval;
			remoteCheckTimer.Start();
		}


		private void RemoteCheck(object sender, EventArgs e)
		{
			viewModel.AutoRemoteCheckAsync().RunInBackground();
		}


		protected override void OnActivated(EventArgs e)
		{
			viewModel.ActivateRefreshAsync().RunInBackground();
		}


		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			viewModel.WindowWith = (int)sizeInfo.NewSize.Width;
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


		private void RemoteAhead_OnClick(object sender, RoutedEventArgs e)
		{
			RemoteAheadContextMenu.PlacementTarget = this;
			RemoteAheadContextMenu.IsOpen = true;
		}


		private void LocalAhead_OnClick(object sender, RoutedEventArgs e)
		{
			LocalAheadContextMenu.PlacementTarget = this;
			LocalAheadContextMenu.IsOpen = true;
		}


		private void Uncommitted_OnClick(object sender, RoutedEventArgs e)
		{
			UncommittedContextMenu.PlacementTarget = this;
			UncommittedContextMenu.IsOpen = true;
		}


		private void MainWindow_OnClosed(object sender, EventArgs e)
		{
			StoreWindowSettings();

			StoreLasteUsedFolder();
		}


		private void StoreWindowSettings()
		{
			WorkFolderSettings settings = Settings.GetWorkFolderSetting(workingFolderService.WorkingFolder);

			settings.Top = Top;
			settings.Left = Left;
			settings.Height = Height;
			settings.Width = Width;
			settings.IsMaximized = WindowState == WindowState.Maximized;
			settings.IsShowCommitDetails = viewModel.RepositoryViewModel.IsShowCommitDetails;

			settings.ShownBranches = viewModel.RepositoryViewModel.Branches
				.Select(b => b.Branch.Name.ToString())
				.ToList();

			Settings.SetWorkFolderSetting(workingFolderService.WorkingFolder, settings);
		}

		private void RestoreWindowSettings(string workingFolder)
		{
			WorkFolderSettings settings = Settings.GetWorkFolderSetting(workingFolder);
			Top = settings.Top;
			Left = settings.Left;
			Height = settings.Height;
			Width = settings.Width;

			WindowState = settings.IsMaximized ? WindowState.Maximized : WindowState.Normal;

			viewModel.RepositoryViewModel.IsShowCommitDetails = settings.IsShowCommitDetails;
		}


		private IReadOnlyList<string> RestoreShownBranches()
		{
			WorkFolderSettings settings = Settings.GetWorkFolderSetting(workingFolderService.WorkingFolder);
			return settings.ShownBranches;
		}


		private void StoreLasteUsedFolder()
		{
			ProgramSettings settings = Settings.Get<ProgramSettings>();
			settings.LastUsedWorkingFolder = workingFolderService.WorkingFolder;
			Settings.Set(settings);
		}


		private static void SetShowToolTipLonger()
		{
			ToolTipService.ShowDurationProperty.OverrideMetadata(
				typeof(DependencyObject), new FrameworkPropertyMetadata(int.MaxValue));
		}
	}
}

