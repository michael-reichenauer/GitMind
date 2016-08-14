using System.Windows;


namespace GitMind.Common.MessageDialogs
{
	public class MessageDialog
	{
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

		public static void ShowInformation(
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
	}
}