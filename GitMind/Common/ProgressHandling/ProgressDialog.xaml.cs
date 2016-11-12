using System.Threading.Tasks;
using System.Windows;


namespace GitMind.Common.ProgressHandling
{
	/// <summary>
	/// Interaction logic for ProgressDialog.xaml
	/// </summary>
	public partial class ProgressDialog : Window
	{
		private readonly IProgressState progressState;
		private readonly Task closeTask;
		private readonly ProgressDialogViewModel viewModel;
		
		internal ProgressDialog(
			Window owner, string text, IProgressState progressState)
		{
			this.progressState = progressState;
			Owner = owner;
			InitializeComponent();

			viewModel = new ProgressDialogViewModel();
			viewModel.Text = text;
			DataContext = viewModel;
		}


		internal ProgressDialog(
			Window owner, 
			string text,
			Task closeTask)
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
			if (closeTask != null)
			{
				viewModel.Start();

				await closeTask;
				viewModel.Stop();
				DialogResult = true;

				return;
			}


			viewModel.Start();
			await progressState.DoAsync(SetText);
		
			viewModel.Stop();
			DialogResult = true;
		}


		public void SetText(string text)
		{
			viewModel.Text = text;
		}
	}
}
