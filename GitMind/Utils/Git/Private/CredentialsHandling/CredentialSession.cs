using System;
using System.Text.RegularExpressions;
using GitMind.Utils.Ipc;


namespace GitMind.Utils.Git.Private.CredentialsHandling
{
	/// <summary>
	/// Handles credentials for a git command. 
	/// If git needs credentials and non of the configured credential helpers like
	/// e.g. git-credential-manager handles the request,
	/// git will try to get credentials from the command line or a defined GIT_ASKPASS program.
	/// So gitCmd will define GIT_ASKPASS to GitMind and thus redirect git to a GitMind instance,
	/// which will forward the request using named pipe to IpcRemotingService started by
	/// this instance. 
	/// </summary>
	internal class CredentialSession : IDisposable
	{
		private readonly ICredentialService credentialService;
		private IpcRemotingService serverSideIpcService;

		private IGitCredential gitCredential = null;


		public CredentialSession(ICredentialService credentialService, string username)
		{
			this.credentialService = credentialService;
			Username = username;

			StartIpcServerSide();
		}


		public string SessionId { get; } = Guid.NewGuid().ToString();

		public bool IsAskPassCanceled { get; private set; } = false;
		public bool IsCredentialRequested { get; private set; } = false;
		public string Username { get; private set; }
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
				Username = gitCredential.Username;
				Log.Debug($"Response: {gitCredential.Password}");
				return Username;
			}

			IsAskPassCanceled = true;
			return null;
		}


		private string HandleGetPassphrase(string resource)
		{
			// Try get cached password och show credential dialog
			if (credentialService.TryGetPassphrase(resource, out gitCredential))
			{
				Username = gitCredential.Username;
				Log.Debug($"Response: {gitCredential.Password}");
				return Username;
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

				string username = Username ?? parsedUsername;

				// Try get cached credential och show credential dialog
				if (credentialService.TryGetCredential(url, username, out gitCredential))
				{
					TargetUri = url;
					Username = gitCredential.Username;
					Log.Debug($"Response: {Username}");
					return Username;
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
			credentialService.SetDialogConfirm(gitCredential, isValid);
		}


		private void StartIpcServerSide()
		{
			// Start IPC server side, which can receive requests from a tmp GitMind process started by 
			// git, when git requires credentials via the GIT_ASKPASS environment vartiable
			serverSideIpcService = new IpcRemotingService();
			serverSideIpcService.TryCreateServer(SessionId);
			serverSideIpcService.PublishService(new CredentialIpcService(this));
		}


		private static void ParseUrl(string totalUrl, out string url, out string username)
		{
			// Start by assuming url does not contain credentials
			url = totalUrl;
			username = null;

			// Checking if url contains credentials in the form of "scheme://credential@host/path"
			int endOfCredentialIndex = totalUrl.LastIndexOf('@');

			if (endOfCredentialIndex != -1)
			{
				// Credential are included in the url just after the scheme, lets extract them
				int schemeIndex = totalUrl.IndexOfOic("://");

				// Getting the url without credential part (i.e. scheme + url)
				int credentialStartIndex = schemeIndex + 3;
				url = totalUrl.Substring(0, credentialStartIndex) + totalUrl.Substring(endOfCredentialIndex + 1);

				// Getting the credentials part (between the scheme and url)
				string credential = totalUrl.Substring(credentialStartIndex, endOfCredentialIndex - credentialStartIndex);

				// In case credentials includes password, we need to split on ":" 
				string[] parts = credential.Split(":".ToCharArray());
				username = parts[0];
			}
		}
	}
}