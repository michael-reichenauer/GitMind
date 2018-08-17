using System;
using System.Threading.Tasks;
using System.Windows;


namespace GitMind.Common.ProgressHandling
{
	/// <summary>
	/// Interaction logic for ProgressDialog.xaml
	/// </summary>
	public partial class ProgressDialog : Window
	{
		private readonly Task closeTask;
		private readonly ProgressDialogViewModel viewModel;


		internal ProgressDialog(Window owner, string text, Task closeTask)
		{
			this.closeTask = closeTask;

			Owner = owner;
			InitializeComponent();

			viewModel = new ProgressDialogViewModel();
			viewModel.Text = text;
			DataContext = viewModel;
		}


		private async void ProgressDialog_OnLoaded(object sender, RoutedEventArgs e)
		{
			viewModel.Start();

			// This dialog was shown async in "using" statement, await the close/dispose before closing
			await closeTask;

			viewModel.Stop();
			Owner?.Activate();
		}


		public void SetText(string text)
		{
			Dispatcher.BeginInvoke((Action)(() => viewModel.Text = text));
		}
	}
}
