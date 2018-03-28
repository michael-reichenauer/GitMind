using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Threading;
using GitMind.Common.Tracking;
using GitMind.MainWindowViews;
using GitMind.Utils.UI;
using GitMind.Utils.UI.Ipc;
using Application = System.Windows.Application;


namespace GitMind.Utils.Git.Private
{
	internal class CredentialSession : IDisposable
	{
		private static readonly string QuitResponse = "quit=true\n";
		private readonly WindowOwner owner;
		private IpcRemotingService serverSideIpcService;

		private CredentialsDialog getCredentialsDialog = null;
		private CredentialCommandData getCommandData = null;

		private CredentialsDialog askPassDialog = null;
		private string askUrl = null;


		public CredentialSession(WindowOwner owner)
		{
			this.owner = owner;

			StartIpcServerSide();
		}


		public string Id { get; } = "AskPassId";    // Guid.NewGuid().ToString();


		public void Dispose()
		{
			serverSideIpcService?.Dispose();
			serverSideIpcService = null;
		}


		public string AskPassRequest(string prompt)
		{
			try
			{
				Log.Debug($"Prompt: {prompt}");

				Match match;
				if ((match = GitAskPassService.AskCredentialRegex.Match(prompt)).Success)
				{
					string seeking = match.Groups[1].Value;
					string totalUrl = match.Groups[2].Value;
					Log.Debug($"Seek: {seeking}, url: {totalUrl}");

					ParseUrl(totalUrl, out string url, out string username);
					Log.Debug($"Url: {url}, username: {username}");

					if (seeking.SameIc("Username"))
					{
						askPassDialog = GetCredential(url, username);
						if (askPassDialog != null)
						{
							askUrl = url;
							Log.Debug($"Response: {askPassDialog.Name}");
							return askPassDialog.Name;
						}

						Log.Debug("Ask password dialog canceled");
					}
					else if (seeking.SameIc("Password") &&
									 askPassDialog != null &&
									 askUrl == url &&
									 username == askPassDialog.Name)
					{
						string password = askPassDialog.Password;
						Log.Debug($"Return password for {username}: {password}");
						return password;
					}
					else
					{
						Log.Debug($"Invalid request");
						askPassDialog = null;
					}
				}
			}
			catch (Exception e)
			{
				Log.Exception(e, "Failed to handle Ask Pass request");
			}

			Log.Debug("No response");
			return "";
		}


		public void ConfirmValidCrededntial(bool isValid)
		{
			Log.Debug($"Confirm valid credentials: {isValid}");
			SetDialogConfirm(askPassDialog, isValid);
		}


		public string CredentialRequest(string command, string commandDataText)
		{
			try
			{
				CredentialCommandData commandData = CredentialCommandData.Parse(commandDataText);

				if (command == "get")
				{
					return HandleGetRequest(commandData);
				}
				else if (command == "store" || command == "erase")
				{
					// Command is store or erase, check if this is expected based on previous get command
					if (IsExpectedCommandData(commandData))
					{
						bool isConfirmed = command == "store";
						SetDialogConfirm(getCredentialsDialog, isConfirmed);
					}
				}
			}
			catch (Exception e)
			{
				Log.Exception(e, "Failed to handle Credential Request");
			}

			return "";
		}


		private void StartIpcServerSide()
		{
			serverSideIpcService = new IpcRemotingService();
			serverSideIpcService.TryCreateServer(Id);
			serverSideIpcService.PublishService(new CredentialIpcService(this));
		}


		private static void ParseUrl(string totallUrl, out string url, out string username)
		{
			// Start by assuming url does not contain credentials
			url = totallUrl;
			username = null;

			// Checking if url contains credentials in the form of "scheme://credential@host/path"
			int endOfCredentialsIndex = totallUrl.LastIndexOf('@');

			if (endOfCredentialsIndex != -1)
			{
				// Credential are included in the url, lets extract them
				int schemeIndex = totallUrl.IndexOf("://");

				// Getting the url without credential part (i.e. scheme + url)
				url = totallUrl.Substring(0, schemeIndex + 3) + totallUrl.Substring(endOfCredentialsIndex + 1);

				// Getting the credentials part (between the scheme and url)
				string credential = totallUrl.Substring(schemeIndex + 3, endOfCredentialsIndex - (schemeIndex + 3));

				// In case credentials includes password, we need to split on ":" 
				string[] parts = credential.Split(":".ToCharArray());
				username = parts[0];
			}
		}


		private string HandleGetRequest(CredentialCommandData commandData)
		{
			if (string.IsNullOrEmpty(commandData.Url))
			{
				return QuitResponse;
			}

			getCommandData = commandData;
			getCredentialsDialog = GetCredential(getCommandData.Url, getCommandData.Username);

			if (getCredentialsDialog == null)
			{
				return QuitResponse;
			}

			string username = getCredentialsDialog.Name;
			string password = getCredentialsDialog.Password;
			return $"{commandData}\nusername={username}\npassword={password}";
		}


		private bool IsExpectedCommandData(CredentialCommandData commandData) =>
			getCommandData != null && getCredentialsDialog != null &&
			getCommandData.Protocol == commandData.Protocol &&
			getCommandData.Host == commandData.Host &&
			getCredentialsDialog.Name == commandData.Username &&
			getCredentialsDialog.Password == commandData.Password;


		private CredentialsDialog GetCredential(string url, string username)
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

			return dialog;
		}


		private void SetDialogConfirm(CredentialsDialog dialog, bool isConfirmed)
		{
			try
			{
				if (dialog == null)
				{
					Log.Debug("No dialog");
					return;
				}

				if (!isConfirmed)
				{
					// Provided credentials where not valid
					Track.Event("CredentialsDialog-Denied");
					dialog.Confirm(false);
				}
				else if (dialog.SaveChecked)
				{
					// Provided credentials where valid and user want them to be cached
					Track.Event("CredentialsDialog-Confirmed");
					dialog.Confirm(true);
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