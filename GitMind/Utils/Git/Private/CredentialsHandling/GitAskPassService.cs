using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GitMind.Utils.Ipc;


namespace GitMind.Utils.Git.Private.CredentialsHandling
{
	/// <summary>
	/// Handles request by the git.exe process to ask for passwords
	/// </summary>
	internal class GitAskPassService : IGitAskPassService
	{
		public static readonly Regex AskCredentialRegex = new Regex(@"(\S+)\s+for\s+['""]([^'""]+)['""]:\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
		public static readonly Regex AskPassphraseRegex = new Regex(@"Enter\s+passphrase\s*for\s*key\s*['""]([^'""]+)['""]\:\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
		public static readonly Regex AskPasswordRegex = new Regex(@"(\S+)'s\s+password:\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
		public static readonly Regex AskAuthenticityRegex = new Regex(@"^\s*The authenticity of host '([^']+)' can't be established.\s+RSA key fingerprint is ([^\s:]+:[^\.]+).", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);


		public bool TryHandleRequest()
		{
			string[] args = Environment.GetCommandLineArgs();

			try
			{
				if (!IsAskPassRequest(args))
				{
					// It was not git.exe password request
					return false;
				}

				HandleAskPasswordRequest(args[1]);
			}
			catch (Exception e)
			{
				Log.Exception(e, $"Failed to handle ask pass request {string.Join("','", args)}");
			}

			return true;
		}


		private static void HandleAskPasswordRequest(string promptText)
		{
			string sessionId = Environment.GetEnvironmentVariable("GITMIND_SESSIONID");

			Log.Debug($"Ask Pass session {sessionId} prompt: '{promptText}'");

			// Sending the request to the "original" GitMind instance that made the git call 
			using (IpcRemotingService ipcRemotingService = new IpcRemotingService())
			{
				// Make the call to the CredentialIpcService
				string response = ipcRemotingService.CallService<CredentialIpcService, string>(
					sessionId, service => service.AskPassRequest(promptText));

				if (response == null)
				{
					Log.Debug("Response: null, Canceled");
					Console.Out.Close();
					return;
				}

				if (!string.IsNullOrEmpty(response))
				{
					Log.Debug("Response: ******");

					// Return the response to the calling git.exe process 
					Console.Write(response);
				}
				else
				{
					Log.Debug("No Response:");
				}
			}
		}


		private static bool IsAskPassRequest(IReadOnlyList<string> args) =>
			args.Count == 2 &&
			(AskCredentialRegex.Match(args[1]).Success ||
			 AskPassphraseRegex.Match(args[1]).Success ||
			 AskPasswordRegex.Match(args[1]).Success ||
			 AskAuthenticityRegex.Match(args[1]).Success);
	}
}