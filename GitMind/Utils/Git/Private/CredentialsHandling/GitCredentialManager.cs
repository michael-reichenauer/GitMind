using System;
using System.Collections.Generic;
using System.IO;
using GitMind.Utils.UI.Ipc;


namespace GitMind.Utils.Git.Private.CredentialsHandling
{
	internal class GitCredentialManager : IGitCredentialManager
	{
		public bool TryHandleCall()
		{
			try
			{
				string[] args = Environment.GetCommandLineArgs();

				if (!IsCredentialRequest(args))
				{
					return false;
				}

				HandleCall(args);
			}
			catch (Exception e)
			{
				Log.Exception(e, "Failed to handle credentials");
			}

			return true;
		}


		private static void HandleCall(string[] args)
		{
			//string command = args[2];
			//string sessionId = args[3];

			//string commandData = ReadCommandData();

			//Log.Debug($"Command: {command}, Id: {sessionId}, {commandData}");

			//using (IpcRemotingService ipcRemotingService = new IpcRemotingService())
			//{
			//	string response = ipcRemotingService.CallService<CredentialIpcService, string>(
			//		sessionId, service => service.CredentialRequest(command, commandData));

			//	if (!string.IsNullOrEmpty(response))
			//	{
			//		// Log.Debug($"Write: {text}");
			//		Console.Write(response);
			//	}
			//}
		}

		private static string ReadCommandData()
		{
			StreamReader inputStream = new StreamReader(Console.OpenStandardInput());
			string inputText = inputStream.ReadToEnd();

			return inputText;
		}


		private static bool IsCredentialRequest(IReadOnlyList<string> args) =>
			args.Count == 4 &&
			args[1] == "--cmg" &&
			(args[2] == "get" || args[2] == "store" || args[2] == "erase");
	}
}