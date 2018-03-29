using System;
using System.Windows.Forms;
using System.Windows.Threading;
using GitMind.Common.Tracking;
using GitMind.MainWindowViews;
using GitMind.Utils.UI;
using Application = System.Windows.Application;


namespace GitMind.Utils.Git.Private
{
	internal class CredentialService : ICredentialService
	{
		private readonly WindowOwner owner;


		public CredentialService(WindowOwner owner)
		{
			this.owner = owner;
		}


		public bool TryGetCredential(string url, string username, out IGitCredential gitCredential)
		{
			string message = $"Enter credentials for: {url}";

			// The key in Windows Credentials Store 
			string targetKey = $"GitMind:{url}";

			CredentialsDialog dialog = null;

			var dispatcher = GetApplicationDispatcher();
			if (dispatcher.CheckAccess())
			{

				dialog = ShowDialog(targetKey, username, message);
			}
			else
			{
				dispatcher.Invoke(() => dialog = ShowDialog(targetKey, username, message));
			}

			if (dialog != null)
			{
				gitCredential = new GitCredential(dialog);
				return true;
			}

			Log.Debug("Credentials dialog canceled");
			gitCredential = null;
			return false;
		}



		public void SetDialogConfirm(IGitCredential gitCredential, bool isConfirmed)
		{
			try
			{
				CredentialsDialog credentialsDialog = (gitCredential as GitCredential)?.Dialog;

				if (credentialsDialog == null)
				{
					Log.Debug("No dialog");
					return;
				}

				if (!isConfirmed)
				{
					// Provided credentials where not valid
					Track.Event("CredentialsDialog-Denied");
					credentialsDialog.Confirm(false);
					bool isDeleted = credentialsDialog.Delete();
					Log.Debug($"Deleted: {isDeleted}");
				}
				else if (credentialsDialog.SaveChecked)
				{
					// Provided credentials where valid and user want them to be cached
					Track.Event("CredentialsDialog-Confirmed");
					credentialsDialog.Confirm(true);
				}
				else
				{
					// User did not want valid credentials to be cached
					Track.Event("CredentialsDialog-NotCached");
				}
			}
			catch (ApplicationException e)
			{
				Log.Exception(e, "");
			}
		}



		private CredentialsDialog ShowDialog(string target, string username, string message)
		{
			CredentialsDialog dialog = new CredentialsDialog(target, "GitMind", message);
			dialog.SaveChecked = true;

			dialog.Name = username;

			// The credential dialog is only shown if there are no cached value for that user
			// The dialog contains a "save" check box, which when checked and credentials have been saved
			// will cache the credentials and thus skip showing the dialog the next time.
			// credentialDialog.AlwaysDisplay = true;
			if (dialog.Show(owner.Win32Window) == DialogResult.OK)
			{
				return dialog;
			}

			Log.Debug($"User canceled {target}, {username}, {message}");
			return null;
		}


		private static Dispatcher GetApplicationDispatcher() =>
			Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
	}
}