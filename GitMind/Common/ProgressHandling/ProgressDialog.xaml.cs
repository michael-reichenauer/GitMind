using System.Windows;


namespace GitMind.Common.ProgressHandling
{
	/// <summary>
	/// Interaction logic for ProgressDialog.xaml
	/// </summary>
	public partial class ProgressDialog : Window
	{
		private readonly IProgressWorker progressWorker;
		private readonly ProgressDialogViewModel viewModel;
		
		internal ProgressDialog(
			Window owner, string text, IProgressWorker progressWorker)
		{
			this.progressWorker = progressWorker;
			Owner = owner;
			InitializeComponent();

			viewModel = new ProgressDialogViewModel();
			viewModel.Text = text;
			DataContext = viewModel;
		}


		private async void ProgressDialog_OnLoaded(object sender, RoutedEventArgs e)
		{
			viewModel.Start();
			await progressWorker.DoAsync(SetText);
		
			viewModel.Stop();
			DialogResult = true;
		}


		private void SetText(string text)
		{
			viewModel.Text = text;
		}
	}
}
