using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace GitMind.RepositoryViews.Open
{
	/// <summary>
	/// Interaction logic for OpenModelView.xaml
	/// </summary>
	public partial class OpenModelView : UserControl
	{
		private OpenRepoViewModel ViewModel => DataContext as OpenRepoViewModel;

		//private MouseClicked mouseClicked;


		public OpenModelView()
		{
			InitializeComponent();

			//mouseClicked = new MouseClicked(this, Clicked);
		}


		private void RecentFile_OnClick(object sender, MouseButtonEventArgs e)
		{
			((sender as FrameworkElement)?.DataContext as FileItem)?.OpenFileCommand.Execute();
			}


		private void OpenFile_OnClick(object sender, MouseButtonEventArgs e)
		{
			ViewModel?.OpenFile();
		}


		private void OpenExample_OnClick(object sender, MouseButtonEventArgs e)
		{
			ViewModel?.OpenExampleFile();
		}
	}
}

