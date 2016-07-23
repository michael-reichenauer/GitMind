using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GitMind.GitModel;
using GitMind.GitModel.Private;
using GitMind.Utils;
using GitMind.Utils.UI.VirtualCanvas;
using UserControl = System.Windows.Controls.UserControl;


namespace GitMind.RepositoryViews
{
	/// <summary>
	/// Interaction logic for RepositoryView.xaml
	/// </summary>
	public partial class RepositoryView : UserControl
	{
		private readonly IRepositoryService repositoryService = new RepositoryService();

		private RepositoryViewModel viewModel;



		public RepositoryView()
		{
			InitializeComponent();
		}



		private void ZoomableCanvas_Loaded(object sender, RoutedEventArgs e)
		{
			viewModel = (RepositoryViewModel)DataContext;
			viewModel.Canvas = (ZoomableCanvas)sender;

			//Timing t = new Timing();

			//Task<Repository> repositoryTask = repositoryService.GetRepositoryAsync(true, viewModel.WorkingFolder);

			//viewModel.Busy.Value.Add(repositoryTask);

			//Repository repository = await repositoryTask;
			//t.Log("Got repository");

			//viewModel.Update(repository, viewModel.SpecifiedBranchNames);
			//t.Log("Updated repositoryViewModel");
			ItemsListBox.Focus();

			//LoadedTime = DateTime.Now;

			//autoRefreshTime.Interval = TimeSpan.FromMilliseconds(300);
			//autoRefreshTime.Start();
		}


		protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
		{
			// Log.Debug($"Canvas offset {canvas.Offset}");

			if (e.ChangedButton == MouseButton.Left)
			{
				Point viewPoint = e.GetPosition(ItemsListBox);

				Point position = new Point(viewPoint.X + viewModel.Canvas.Offset.X, viewPoint.Y + viewModel.Canvas.Offset.Y);

				bool isControl = (Keyboard.Modifiers & ModifierKeys.Control) > 0;

				viewModel.Clicked(position, isControl);
			}

			base.OnPreviewMouseUp(e);
		}


		private void MouseDobleClick(object sender, MouseButtonEventArgs e)
		{
			Point viewPoint = e.GetPosition(ItemsListBox);
			if (viewPoint.X > viewModel.GraphWidth)
			{
				viewModel.ToggleDetailsCommand.Execute(null);
			}
		}


		private void MouseEntering(object sender, MouseEventArgs e)
		{
			ListBoxItem item = sender as ListBoxItem;
			if (item != null)
			{
				BranchViewModel branch = item.Content as BranchViewModel;
				if (branch != null)
				{
					viewModel.MouseEnterBranch(branch);
				}
			}			
		}


		private void MouseLeaving(object sender, MouseEventArgs e)
		{
			ListBoxItem item = sender as ListBoxItem;
			if (item != null)
			{
				BranchViewModel branch = item.Content as BranchViewModel;
				if (branch != null)
				{
					viewModel.MouseLeaveBranch(branch);
				}
			}
		}
	}
}
