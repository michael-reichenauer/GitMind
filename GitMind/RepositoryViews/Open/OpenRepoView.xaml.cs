using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace GitMind.RepositoryViews.Open
{
	/// <summary>
	/// Interaction logic for OpenRepoView.xaml
	/// </summary>
	public partial class OpenRepoView : UserControl
	{
		private OpenRepoViewModel ViewModel => DataContext as OpenRepoViewModel;


		public OpenRepoView()
		{
			InitializeComponent();
		}


		private void RecentFile_OnClick(object sender, MouseButtonEventArgs e)
		{
			((sender as FrameworkElement)?.DataContext as FileItem)?.OpenFileCommand.Execute();
		}


		private void OpenRepo_OnClick(object sender, MouseButtonEventArgs e) => ViewModel?.OpenRepoAsync();


		private void Clone_OnClick(object sender, MouseButtonEventArgs e) => ViewModel?.CloneRepoAsync();


		private void Init_OnClick(object sender, MouseButtonEventArgs e) => ViewModel?.InitRepoAsync();
	}
}

