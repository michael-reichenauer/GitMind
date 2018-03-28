using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GitMind.Utils.UI.Ipc;


namespace GitMind.Utils.Git.Private
{
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
					return false;
				}

				AskPass(args[1]);
			}
			catch (Exception e)
			{
				Log.Exception(e, $"Failed to handle ask pass request {string.Join("','", args)}");
			}

			return true;
		}


		private void AskPass(string promptText)
		{
			Log.Debug($"Ask Pass prompt: '{promptText}'");

			string sessionId = "AskPassId";
			using (IpcRemotingService ipcRemotingService = new IpcRemotingService())
			{
				string response = ipcRemotingService.CallService<CredentialIpcService, string>(
					sessionId, service => service.AskPassRequest(promptText));

				if (!string.IsNullOrEmpty(response))
				{
					Log.Debug("Response: ******");
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