using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using GitMind.Utils;


namespace GitMind.ApplicationHandling.Installation
{
	/// <summary>
	/// Interaction logic for MessageDialog.xaml
	/// </summary>
	public partial class InstallDialog : Window
	{
		private InstallDialogViewModel viewModel;

		public InstallDialog(
			Window owner,
			string message,
			string title,
			Func<Task> installActionAsync)
		{
			Owner = owner;
			InitializeComponent();

			viewModel = new InstallDialogViewModel(installActionAsync);
			DataContext = viewModel;

			viewModel.Title = title;
			viewModel.Message = message;
		}


		public string Message { get => viewModel.Message; set => viewModel.Message = value; }

		public bool IsButtonsVisible { get => viewModel.IsButtonsVisible; set => viewModel.IsButtonsVisible = value; }
	}
}
