using System.Media;
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
				viewModel.OkText = "Ok";
				viewModel.IsCancelVisible = false;
			}
			else if (button == MessageBoxButton.OKCancel)
			{
				viewModel.OkText = "Ok";
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

		public static void ShowInfo(
		string message,
		string title = null)
		{
			MessageDialog dialog = new MessageDialog(
				null,
				message,
				title ?? "GitMind",
				MessageBoxButton.OK,
				MessageBoxImage.Information);

			dialog.ShowDialog();
		}


		public static void ShowInfo(
			Window owner,
			string message,
			string title = null)
		{
			MessageDialog dialog = new MessageDialog(
				owner,
				message,
				title ?? "GitMind",
				MessageBoxButton.OK,
				MessageBoxImage.Information);

			dialog.ShowDialog();
		}

		public static bool ShowAskOkCancel(
			string message,
			string title = null)
		{
			MessageDialog dialog = new MessageDialog(
				null,
				message,
				title ?? "GitMind",
				MessageBoxButton.OKCancel,
				MessageBoxImage.Question);

			return dialog.ShowDialog() == true;
		}


		public static bool ShowAskOkCancel(
			Window owner,
			string message,
			string title = null)
		{
			MessageDialog dialog = new MessageDialog(
				owner,
				message,
				title ?? "GitMind",
				MessageBoxButton.OKCancel,
				MessageBoxImage.Question);

			return dialog.ShowDialog() == true;
		}


		public static void ShowWarning(
			Window owner,
			string message,
			string title = null)
		{
			MessageDialog dialog = new MessageDialog(
				owner,
				message,
				title ?? "GitMind",
				MessageBoxButton.OK,
				MessageBoxImage.Warning);

			dialog.ShowDialog();
		}

		public static bool ShowWarningAskYesNo(Window owner, string message, string title = null)
		{
			MessageDialog dialog = new MessageDialog(
				owner,
				message,
				title ?? "GitMind",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning);

			return dialog.ShowDialog() == true;
		}

		public static void ShowError(
			Window owner,
			string message,
			string title = null)
		{
			MessageDialog dialog = new MessageDialog(
				owner,
				message,
				title ?? "GitMind",
				MessageBoxButton.OK,
				MessageBoxImage.Error);

			SystemSounds.Beep.Play();
			dialog.ShowDialog();
		}
	}
}
