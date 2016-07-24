using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GitMind.Utils;


namespace GitMind.MainWindowViews
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private static readonly TimeSpan AutoRefreshInterval = TimeSpan.FromMinutes(1);
		private static readonly TimeSpan OnActivatedInterval = TimeSpan.FromSeconds(10);

		private readonly DispatcherTimer autoRefreshTimer = new DispatcherTimer();

		private readonly MainWindowViewModel viewModel;
		private DateTime ActivatedTime = DateTime.MaxValue;


		public MainWindow()
		{
			InitializeComponent();

			// Make sure maximize window does not cover the task bar
			MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight - 8;

			autoRefreshTimer.Tick += AutoRefresh;
			autoRefreshTimer.Interval = AutoRefreshInterval;

			viewModel = new MainWindowViewModel(this);
			DataContext = viewModel;

			Activate();
		}


		public string WorkingFolder {set { viewModel.WorkingFolder = value; } }

		public IReadOnlyList<string> BranchNames { set { viewModel.SpecifiedBranchNames = value; } }


		public bool IsNewVersionVisible
		{
			set { viewModel.IsNewVersionVisible = value; }
		}


		private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			await viewModel.FirstLoadAsync();
			ActivatedTime = DateTime.Now;

			autoRefreshTimer.Start();
		}



		private void AutoRefresh(object sender, EventArgs e)
		{
			try
			{
				viewModel.AutoRefreshAsync().RunInBackground();
			}
			catch (Exception ex) when (ex.IsNotFatal())
			{
				Log.Error($"Failed to auto refresh {ex}");
			}
		}


		protected override void OnActivated(EventArgs e)
		{
			if (ActivatedTime < DateTime.MaxValue && DateTime.Now - ActivatedTime > OnActivatedInterval)
			{
				Log.Debug("Refreshing after activation");
				viewModel.ActivateRefreshAsync().RunInBackground();
				ActivatedTime = DateTime.Now;
			}
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
	}
}

