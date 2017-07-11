using System;
using System.Net;
using System.Windows.Forms;
using System.Windows.Threading;
using GitMind.Common.Tracking;
using GitMind.Git;
using GitMind.MainWindowViews;
using GitMind.Utils;
using GitMind.Utils.UI;
using Application = System.Windows.Application;


namespace GitMind.Common
{
	internal class CredentialHandler : ICredentialHandler
	{
		private readonly WindowOwner owner;

		private CredentialsDialog dialog;
		private NetworkCredential networkCredential = null;


		public CredentialHandler(WindowOwner owner)
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
				Track.Event("CredentialsDialog-Confirmed");
				dialog.Confirm(true);				
			}
			else if (dialog.SaveChecked)
			{
				try
				{
					Track.Event("CredentialsDialog-Denied");
					dialog.Confirm(false);
				}
				catch (ApplicationException e)
				{
					Log.Exception(e, "");
				}
			}
		}



		private void ShowDialog(string target, string usernameFromUrl, string message)
		{
			networkCredential = null;
			dialog = new CredentialsDialog(target, "GitMind", message);

			// dialog.AlwaysDisplay = true;

			Track.Event("CredentialsDialog-Show");
			dialog.Name = usernameFromUrl;

			if (dialog.Show(owner.Win32Window) == DialogResult.OK)
			{
				networkCredential = new NetworkCredential(dialog.Name, dialog.Password);
			}
		}


		private static Dispatcher GetApplicationDispatcher() =>
			Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
	}
}