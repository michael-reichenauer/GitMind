using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GitMind.Utils.UI.VirtualCanvas;
using UserControl = System.Windows.Controls.UserControl;


namespace GitMind.RepositoryViews
{
	/// <summary>
	/// Interaction logic for RepositoryView.xaml
	/// </summary>
	public partial class RepositoryView : UserControl
	{
		private RepositoryViewModel viewModel;


		public RepositoryView()
		{
			InitializeComponent();
		}


		private void ZoomableCanvas_Loaded(object sender, RoutedEventArgs e)
		{
			viewModel = (RepositoryViewModel)DataContext;
			viewModel.Canvas = (ZoomableCanvas)sender;
			viewModel.ListBox = ItemsListBox;

			ItemsListBox.Focus();
		}


		protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
		{
			// Log.Debug($"Canvas offset {canvas.Offset}");

			if (e.ChangedButton == MouseButton.Left)
			{
				Point viewPoint = e.GetPosition(ItemsListBox);

				Point position = new Point(viewPoint.X + viewModel.Canvas.Offset.X, viewPoint.Y + viewModel.Canvas.Offset.Y);

				viewModel.Clicked(position);
			}

			base.OnPreviewMouseUp(e);
		}


		private void MouseDobleClick(object sender, MouseButtonEventArgs e)
		{
			Point viewPoint = e.GetPosition(ItemsListBox);
			if (viewPoint.X > viewModel.GraphWidth)
			{
				viewModel.ToggleDetailsCommand.Execute();
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

				CommitViewModel commit = item.Content as CommitViewModel;
				if (commit != null)
				{
					Point viewPoint = e.GetPosition(ItemsListBox);
					if (viewPoint.X < viewModel.GraphWidth)
					{
						branch = viewModel.Branches.FirstOrDefault(b => b.Branch == commit.Commit.Branch);
						if (branch != null)
						{
							viewModel.MouseEnterBranch(branch);
						}
					}

					if (viewPoint.X > viewModel.GraphWidth)
					{
						branch = viewModel.Branches.FirstOrDefault(b => b.Branch == commit.Commit.Branch);
						if (branch != null)
						{
							viewModel.MouseLeaveBranch(branch);
						}

					}
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

				CommitViewModel commit = item.Content as CommitViewModel;
				if (commit != null)
				{
					Point viewPoint = e.GetPosition(ItemsListBox);
					if (viewPoint.X < viewModel.GraphWidth)
					{
						branch = viewModel.Branches.FirstOrDefault(b => b.Branch == commit.Commit.Branch);
						if (branch != null)
						{
							viewModel.MouseLeaveBranch(branch);
						}
					}
				}
			}
		}


		private void EventMouseUp(object sender, MouseButtonEventArgs e)
		{
			ListBoxItem item = sender as ListBoxItem;
			if (item != null)
			{
				BranchViewModel branch = item.Content as BranchViewModel;
				if (branch != null)
				{

				}
			}
		}
	}
}
