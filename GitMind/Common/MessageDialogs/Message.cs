using System.Media;
using System.Windows;


namespace GitMind.Common.MessageDialogs
{
	internal static class Message
	{
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
			string message,
			string title = null)
		{
			MessageDialog dialog = new MessageDialog(
				null,
				message,
				title ?? "GitMind",
				MessageBoxButton.OK,
				MessageBoxImage.Error);

			SystemSounds.Beep.Play();
			dialog.ShowDialog();
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