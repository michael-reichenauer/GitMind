using System;
using System.Net;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Threading;
using GitMind.Git;
using GitMind.Utils;
using Application = System.Windows.Application;


namespace GitMind.RepositoryViews
{
	internal class CredentialHandler : ICredentialHandler
	{
		private readonly Window owner;

		private CredentialsDialog dialog;
		private NetworkCredential networkCredential = null;


		public CredentialHandler(Window owner)
		{
			this.owner = owner;
		}


		public NetworkCredential GetCredential(string url, string usernameFromUrl)
		{
			Uri uri = null;
			string target = null;
			if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out uri))
			{
				target = uri.Host;
			}

			string message = $"Enter credentials for: {target ?? url}";

			var dispatcher = GetApplicationDispatcher();
			if (dispatcher.CheckAccess())
			{
				ShowDialog(target, usernameFromUrl, message);
			}
			else
			{
				dispatcher.Invoke(() => ShowDialog(target, usernameFromUrl, message));
			}

			return networkCredential;
		}


		public void SetConfirm(bool isConfirmed)
		{
			if (dialog == null)
			{
				return;
			}

			if (isConfirmed && dialog.SaveChecked)
			{
				dialog.Confirm(true);
			}
			else
			{
				try
				{
					dialog.Confirm(false);
				}
				catch (ApplicationException e)
				{
					Log.Warn($"Error {e}");
				}
			}
		}


		private void ShowDialog(string target, string usernameFromUrl, string message)
		{
			System.Windows.Forms.IWin32Window ownerHandle = new WindowWrapper(owner);

			networkCredential = null;
			dialog = new CredentialsDialog(target, "GitMind", message);

			dialog.Name = usernameFromUrl;
			if (dialog.Show(ownerHandle) == DialogResult.OK)
			{
				networkCredential = new NetworkCredential(dialog.Name, dialog.Password);
			}
		}


		private static Dispatcher GetApplicationDispatcher() =>
			Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;


		//private static bool Login(string name)
		//{
		//	bool value = false;
		//	try
		//	{
		//		CredentialsDialog dialog = new CredentialsDialog("<target>", "<caption>", "<message>");
		//		//if (name != null) dialog.AlwaysDisplay = true; // prevent an infinite loop
		//		if (dialog.Show() == DialogResult.OK)
		//		{
		//			//if (Authenticate(dialog.Name, dialog.Password))
		//			//{
		//			//	value = true;
		//			//	if (dialog.SaveChecked) dialog.Confirm(true);
		//			//}
		//			//else
		//			//{
		//			//	try
		//			//	{
		//			//		dialog.Confirm(false);;
		//			//	}
		//			//	catch (ApplicationException e)
		//			//	{
		//			//		Log.Warn($"Error {e}");
		//			//	}

		//			//	value = Login(dialog.Name); // need to find a way to display 'Logon unsuccessful'
		//			//}
		//		}
		//	}
		//	catch (ApplicationException e)
		//	{
		//		Log.Warn($"Error {e}");
		//	}
		//	return value;
		//}


		private class WindowWrapper : System.Windows.Forms.IWin32Window
		{
			public WindowWrapper(Window window)
			{
				Handle = new WindowInteropHelper(window).Handle;
			}

			public IntPtr Handle { get; }
		}
	}
}