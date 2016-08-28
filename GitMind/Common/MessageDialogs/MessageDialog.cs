using System.Windows;


namespace GitMind.Common.MessageDialogs
{
	public class MessageDialog
	{
		public static void ShowInfo(
			string message,
			string title = null)
		{
			MessageBox.Show(
				message,
				title ?? "GitMind",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}


		public static void ShowInfo(
			Window owner,
			string message,
			string title = null)
		{
			MessageBox.Show(
				owner,
				message,
				title ?? "GitMind",
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}

		public static bool ShowAskOkCancel(
			string message,
			string title = null)
		{
			return MessageBoxResult.OK == MessageBox.Show(
				message,
				title ?? "GitMind",
				MessageBoxButton.OKCancel,
				MessageBoxImage.Question);
		}


		public static bool ShowAskOkCancel(
			Window owner,
			string message,
			string title = null)
		{
			return MessageBoxResult.OK == MessageBox.Show(
				owner,
				message,
				title ?? "GitMind",
				MessageBoxButton.OKCancel,
				MessageBoxImage.Question);
		}


		public static void ShowWarning(
			Window owner,
			string message,
			string title= null)
		{
			MessageBox.Show(
				owner,
				message,
				title ?? "GitMind",
				MessageBoxButton.OK,
				MessageBoxImage.Warning);
		}


		public static bool ShowWarningAskYesNo(Window owner, string message, string title = null)
		{
			return MessageBoxResult.Yes == MessageBox.Show(
				owner,
				message,
				title ?? "GitMind",
				MessageBoxButton.YesNo,
				MessageBoxImage.Warning,
				MessageBoxResult.No);
		}

		public static void ShowError(
			Window owner,
			string message,
			string title = null)
		{
			MessageBox.Show(
				owner,
				message,
				title ?? "GitMind",
				MessageBoxButton.OK,
				MessageBoxImage.Error);
		}
	}
}