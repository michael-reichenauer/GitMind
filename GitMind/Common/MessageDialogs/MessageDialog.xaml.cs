using System.Windows;


namespace GitMind.Common.MessageDialogs
{
	/// <summary>
	/// Interaction logic for MessageDialog.xaml
	/// </summary>
	public partial class MessageDialog : Window
	{
		public MessageDialog(
			Window owner,
			string message,
			string title,
			MessageBoxButton button,
			MessageBoxImage image)
		{
			Owner = owner;
			InitializeComponent();

			MessageDialogViewModel viewModel = new MessageDialogViewModel();
			DataContext = viewModel;

			viewModel.Title = title;
			viewModel.Message = message;

			viewModel.IsInfo = image == MessageBoxImage.Information;
			viewModel.IsQuestion = image == MessageBoxImage.Question;
			viewModel.IsWarn = image == MessageBoxImage.Warning;
			viewModel.IsError = image == MessageBoxImage.Error;

			if (button == MessageBoxButton.OK)
			{
				viewModel.OkText = "OK";
				viewModel.IsCancelVisible = false;
			}
			else if (button == MessageBoxButton.OKCancel)
			{
				viewModel.OkText = "OK";
				viewModel.CancelText = "Cancel";
				viewModel.IsCancelVisible = true;
			}
			else if (button == MessageBoxButton.YesNo)
			{
				viewModel.OkText = "Yes";
				viewModel.CancelText = "No";
				viewModel.IsCancelVisible = true;
			}
		}
	}
}
