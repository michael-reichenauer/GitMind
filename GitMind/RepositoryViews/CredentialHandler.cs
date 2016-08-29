using System;
using System.Net;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using GitMind.Git;


namespace GitMind.RepositoryViews
{
	internal class CredentialHandler : ICredentialHandler
	{
		private readonly Window owner;


		public CredentialHandler(Window owner)
		{
			this.owner = owner;
		}


		public NetworkCredential GetCredential(string url, string usernameFromUrl)
		{
			string message = $"Enter credentials for {url}";

			System.Windows.Forms.IWin32Window ownerHandle = new WindowWrapper(owner);

			CredentialsDialog dialog = new CredentialsDialog("<target>", "GitMind", message);

			dialog.Name = usernameFromUrl;
			if (dialog.Show(ownerHandle) == DialogResult.OK)
			{
				return new NetworkCredential(dialog.Name, dialog.Password);
			}

			return null;
		}


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
		//			//		dialog.Confirm(false);
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