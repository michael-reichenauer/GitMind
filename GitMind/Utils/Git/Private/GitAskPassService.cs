using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace GitMind.Utils.Git.Private
{
	internal class GitAskPassService : IGitAskPassService
	{
		private static readonly Regex AskCredentialRegex = new Regex(@"(\S+)\s+for\s+['""]([^'""]+)['""]:\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
		private static readonly Regex AskPassphraseRegex = new Regex(@"Enter\s+passphrase\s*for\s*key\s*['""]([^'""]+)['""]\:\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
		private static readonly Regex AskPasswordRegex = new Regex(@"(\S+)'s\s+password:\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
		private static readonly Regex AskAuthenticityRegex = new Regex(@"^\s*The authenticity of host '([^']+)' can't be established.\s+RSA key fingerprint is ([^\s:]+:[^\.]+).", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

		private readonly IGitPromptService gitPromptService;


		public GitAskPassService(IGitPromptService gitPromptService)
		{
			this.gitPromptService = gitPromptService;
		}


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

			if (AskAuthenticityRegex.Match(promptText).Success)
			{
				// Git prompts for yes or no answer
				if (gitPromptService.TryPromptYesNo(promptText))
				{
					Log.Debug("Approved authorization of host.");
					Console.Out.Write("yes\n");
				}
				else
				{
					Log.Debug("Denied authorization of host.");
					Console.Out.Write("no\n");
				}

				return;
			}


			// Git prompts from some text
			if (gitPromptService.TryPromptText(promptText, out string response))
			{
				Log.Debug("Response acquired.");

				Console.Out.Write(response ?? "" + "\n");
			}
			else
			{
				Log.Debug("Ask pass canceled.");
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