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

			// Store the canvas in a local variable since x:Name doesn't work.
			Timing t = new Timing();

			string workingFolder = TestRepo.Path4;

			R<string> path = ProgramPaths.GetWorkingFolderPath(workingFolder);

			while (!path.HasValue)
			{
				Log.Warn($"Not a valid working folder '{workingFolder}'");

				var dialog = new FolderBrowserDialog();
				dialog.Description = "Select a working folder with a valid git repository.";
				dialog.ShowNewFolderButton = false;
				dialog.SelectedPath = Environment.CurrentDirectory;
				if (dialog.ShowDialog(this.GetIWin32Window()) != DialogResult.OK)
				{
					Log.Warn("User canceled selecting a Working folder");
					Application.Current.Shutdown(0);
					return;
				}

				path = ProgramPaths.GetWorkingFolderPath(dialog.SelectedPath);
			}

			ProgramSettings.SetLatestUsedWorkingFolderPath(path.Value);
			workingFolder = path.Value;
		//	mainWindowViewModel.WorkingFolder = workingFolder;

			t.Log("Got working folder");
			Task<Repository> repositoryTask = repositoryService.GetRepositoryAsync(true, workingFolder);

			//mainWindowViewModel.Busy.Add(repositoryTask);

			Repository repository = await repositoryTask;
			t.Log("Got repository");
			List<string> specifiedBranchNames = new List<string>();
			viewModel.Update(repository, specifiedBranchNames);
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
