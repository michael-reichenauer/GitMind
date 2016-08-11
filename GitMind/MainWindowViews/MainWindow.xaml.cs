using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GitMind.Features.FolderMonitoring;
using GitMind.Utils;


namespace GitMind.MainWindowViews
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private static readonly TimeSpan remoteCheckInterval = TimeSpan.FromMinutes(10);
		private static readonly TimeSpan OnActivatedInterval = TimeSpan.FromSeconds(10);

		private readonly DispatcherTimer remoteCheckTimer = new DispatcherTimer();

		private readonly MainWindowViewModel viewModel;
	


		public MainWindow()
		{
			InitializeComponent();

			// Make sure maximize window does not cover the task bar
			MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight - 8;

			remoteCheckTimer.Tick += RemoteCheck;
			remoteCheckTimer.Interval = remoteCheckInterval;

			viewModel = new MainWindowViewModel(this);
			DataContext = viewModel;

			Activate();
		}




		public string WorkingFolder
		{
			set {viewModel.WorkingFolder = value;}
		}


		public IReadOnlyList<string> BranchNames { set { viewModel.SpecifiedBranchNames = value; } }


		public bool IsNewVersionVisible
		{
			set { viewModel.IsNewVersionVisible = value; }
		}


		private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			await viewModel.FirstLoadAsync();

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
	}
}

