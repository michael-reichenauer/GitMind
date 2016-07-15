using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GitMind.GitModel;
using GitMind.GitModel.Private;
using GitMind.Settings;
using GitMind.Utils;
using GitMind.VirtualCanvas;
using Application = System.Windows.Application;
using UserControl = System.Windows.Controls.UserControl;


namespace GitMind.CommitsHistory
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



		private async void ZoomableCanvas_Loaded(object sender, RoutedEventArgs e)
		{
			viewModel = (RepositoryViewModel)DataContext;
			viewModel.Canvas = (ZoomableCanvas)sender;

			Timing t = new Timing();

			Task<Repository> repositoryTask = repositoryService.GetRepositoryAsync(true, viewModel.WorkingFolder);

			viewModel.Busy.Value.Add(repositoryTask);

			Repository repository = await repositoryTask;
			t.Log("Got repository");

			viewModel.Update(repository, viewModel.SpecifiedBranchNames);
			t.Log("Updated repositoryViewModel");
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
	}
}
