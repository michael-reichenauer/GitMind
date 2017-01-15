using System.Media;
using System.Windows;
using GitMind.MainWindowViews;


namespace GitMind.Common.MessageDialogs
{
	internal interface IMessage
	{
		void ShowInfo(string message, string title = null);

		bool ShowAskOkCancel(string message, string title = null);

		void ShowWarning(string message, string title = null);

		bool ShowWarningAskYesNo(string message, string title = null);

		void ShowError(string message, string title = null);
	}


	internal class IMessageService : IMessage
	{
		private readonly WindowOwner owner;

		public IMessageService(WindowOwner owner)
		{
			this.owner = owner;
		}


		public void ShowInfo(string message, string title = null)
		{
			Show(message,title ,MessageBoxButton.OK,MessageBoxImage.Information);
		}


		public bool ShowAskOkCancel(string message, string title = null)
		{
			return Show(message, title, MessageBoxButton.OKCancel, MessageBoxImage.Question) == true;
		}


		public void ShowWarning(string message, string title = null)
		{
			Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
		}

		public bool ShowWarningAskYesNo(string message, string title = null)
		{
			return Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == true;
		}


		public void ShowError(string message, string title = null)
		{
			SystemSounds.Beep.Play();
			Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
		}


		private bool? Show(string text, string title, MessageBoxButton button, MessageBoxImage image)
		{
			MessageDialog dialog = new MessageDialog(
				owner,
				text,
				title ?? "GitMind",
				button,
				image);

			return dialog.ShowDialog();
		}
	}


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