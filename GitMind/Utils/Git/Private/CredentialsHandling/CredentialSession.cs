using System;
using System.Text.RegularExpressions;
using GitMind.Utils.Ipc;


namespace GitMind.Utils.Git.Private.CredentialsHandling
{
	internal class CredentialSession : IDisposable
	{
		private readonly ICredentialService credentialService;
		private IpcRemotingService serverSideIpcService;

		private IGitCredential gitCredential = null;


		public CredentialSession(ICredentialService credentialService, string username)
		{
			this.credentialService = credentialService;
			LastUsername = username;

			StartIpcServerSide();
		}


		public string Id { get; } = Guid.NewGuid().ToString();

		public bool IsAskPassCanceled { get; private set; } = false;
		public bool IsCredentialRequested { get; private set; } = false;
		public string LastUsername { get; private set; }
		public object TargetUri { get; private set; }


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
					return HandleGetCredential(seeking, totalUrl);
				}
				else if ((match = GitAskPassService.AskPasswordRegex.Match(prompt)).Success)
				{
					string resource = match.Groups[1].Value;
					return HandleGetPassword(resource);
				}
				else if ((match = GitAskPassService.AskPassphraseRegex.Match(prompt)).Success)
				{
					string resource = match.Groups[1].Value;
					return HandleGetPassphrase(resource);
				}
			}
			catch (Exception e)
			{
				Log.Exception(e, "Failed to handle Ask Pass request");
			}

			Log.Debug("No response");
			return "";
		}


		private string HandleGetPassword(string username)
		{
			// Try get cached password och show credential dialog
			if (credentialService.TryGetPassword(username, out gitCredential))
			{
				LastUsername = gitCredential.Username;
				Log.Debug($"Response: {gitCredential.Password}");
				return LastUsername;
			}

			IsAskPassCanceled = true;
			return null;
		}


		private string HandleGetPassphrase(string resource)
		{
			// Try get cached password och show credential dialog
			if (credentialService.TryGetPassphrase(resource, out gitCredential))
			{
				LastUsername = gitCredential.Username;
				Log.Debug($"Response: {gitCredential.Password}");
				return LastUsername;
			}

			IsAskPassCanceled = true;
			return null;
		}


		public string HandleGetCredential(string seeking, string totalUrl)
		{
			// This function will be called 2 times. The first time is a request for UserName. 
			// However, we try to get both user name and password and then store credential in memory
			// so the password can be returned in the second call without bothering the user again.
			// The total url may contain some parts (user name) of the credential, lets try extract that.
			ParseUrl(totalUrl, out string url, out string parsedUsername);

			if (seeking.SameIc("Username"))
			{
				// First call requesting the UserName
				IsCredentialRequested = true;
				TargetUri = url;

				string username = LastUsername ?? parsedUsername;

				// Try get cached credential och show credential dialog
				if (credentialService.TryGetCredential(url, username, out gitCredential))
				{
					TargetUri = url;
					LastUsername = gitCredential.Username;
					Log.Debug($"Response: {LastUsername}");
					return LastUsername;
				}

				IsAskPassCanceled = true;
				return null;
			}
			else if (seeking.SameIc("Password"))
			{
				// Second call requesting the Password. Lets ensure it is for the stored Username in previous call
				if (gitCredential != null &&
						url == gitCredential.Url &&
						parsedUsername == gitCredential.Username)
				{
					string password = gitCredential.Password;
					Log.Debug($"Response: <password for {parsedUsername}>");
					return password;
				}
			}
			else
			{
				Log.Debug("Invalid request");
				gitCredential = null;
			}

			Log.Debug("No response");
			return null;
		}

		public void ConfirmValidCrededntial(bool isValid)
		{
			Log.Debug($"Confirm valid credentials: {isValid}");

			credentialService.SetDialogConfirm(gitCredential, isValid);
		}


		private void StartIpcServerSide()
		{
			// Start IPC server side, which can receive requests from a tmp GitMind process started by 
			// git, when git requires credentials
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
			int endOfCredentialIndex = totallUrl.LastIndexOf('@');

			if (endOfCredentialIndex != -1)
			{
				// Credential are included in the url just after the scheme, lets extract them
				int schemeIndex = totallUrl.IndexOfOic("://");

				// Getting the url without credential part (i.e. scheme + url)
				int credentialStartIndex = schemeIndex + 3;
				url = totallUrl.Substring(0, credentialStartIndex) + totallUrl.Substring(endOfCredentialIndex + 1);

				// Getting the credentials part (between the scheme and url)
				string credential = totallUrl.Substring(credentialStartIndex, endOfCredentialIndex - credentialStartIndex);

				// In case credentials includes password, we need to split on ":" 
				string[] parts = credential.Split(":".ToCharArray());
				username = parts[0];
			}
		}



		//private static readonly string QuitResponse = "quit=true\n";
		//private CredentialsDialog getCredentialsDialog = null;
		//private CredentialCommandData getCommandData = null;


		//public string CredentialRequest(string command, string commandDataText)
		//{
		//	try
		//	{
		//		CredentialCommandData commandData = CredentialCommandData.Parse(commandDataText);

		//		if (command == "get")
		//		{
		//			return HandleGetRequest(commandData);
		//		}
		//		else if (command == "store" || command == "erase")
		//		{
		//			// Command is store or erase, check if this is expected based on previous get command
		//			if (IsExpectedCommandData(commandData))
		//			{
		//				bool isConfirmed = command == "store";
		//				credentialService.SetDialogConfirm(getCredentialsDialog, isConfirmed);
		//			}
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		Log.Exception(e, "Failed to handle Credential Request");
		//	}

		//	return "";
		//}



		//private string HandleGetRequest(CredentialCommandData commandData)
		//{
		//	if (string.IsNullOrEmpty(commandData.Url))
		//	{
		//		return QuitResponse;
		//	}

		//	getCommandData = commandData;
		//	getCredentialsDialog = credentialService.GetCredential(getCommandData.Url, getCommandData.Username);

		//	if (getCredentialsDialog == null)
		//	{
		//		return QuitResponse;
		//	}

		//	string username = getCredentialsDialog.Name;
		//	string password = getCredentialsDialog.Password;
		//	return $"{commandData}\nusername={username}\npassword={password}";
		//}


		//private bool IsExpectedCommandData(CredentialCommandData commandData) =>
		//	getCommandData != null && getCredentialsDialog != null &&
		//	getCommandData.Protocol == commandData.Protocol &&
		//	getCommandData.Host == commandData.Host &&
		//	getCredentialsDialog.Name == commandData.Username &&
		//	getCredentialsDialog.Password == commandData.Password;
	}
}