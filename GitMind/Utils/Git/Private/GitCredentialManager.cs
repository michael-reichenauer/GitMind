using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;


namespace GitMind.Utils.Git.Private
{
	internal class GitCredentialManager : IGitCredentialManager
	{
		private static readonly char[] QuoteChar = "\"".ToCharArray();
		private string CredentialsMgrPath => Path.Combine(
				gitInfo.GetGitPathAsync(CancellationToken.None).Result, "git-credential-manager.exe");

		private readonly IGitConfig gitConfig;
		private readonly IGitInfo gitInfo;



		public GitCredentialManager(
			IGitConfig gitConfig,
			IGitInfo gitInfo)
		{
			this.gitConfig = gitConfig;
			this.gitInfo = gitInfo;
		}


		public bool TryHandleCall()
		{
			try
			{
				string[] args = Environment.GetCommandLineArgs();

				if (!IsCredentialCall(args))
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



		private void HandleCall(string[] args)
		{
			string command = args[2];
			Log.Debug($"Command: {command}");

			string commandRequest = ReadCommandRequestText();
			if (command == "get")
			{
				Log.Debug($"Input for get:\n{commandRequest}");
				Write("quit=true\n");
				//Log.Debug($"Return no credentials");
				//Write(commandRequest);
				//Write($"username=\n");
				//Write($"password=\n");
				return;
			}

			return;


			//gitConfig.TryGet("credential.helper", out GitSetting helper);
			//Track.Info($"credential.helper: '{helper}'");

			//if (helper == null || helper.Values.All(v => v == "!GitMind.exe"))
			//{
			//	// No configured credential manager, lest start builtin git crededential manager
			//	Log.Debug("No configured credential manager, call git provided manager");
			//	Process process = StartCredentialsManager(command);

			//	WriteCommandRequestText($"{commandRequest}\n", process);

			//	if (command == "get")
			//	{
			//		string outputText = ReadCommandResponseText(process);
			//		// Log.Debug($"Output:\n{outputText}");

			//		WriteCommandResponseText(outputText);
			//	}

			//	process.WaitForExit();
			//	Log.Debug($"Exit code {process.ExitCode}");
			//	process.Close();

			//	return;
			//}
			//else
			//{
			//	// None of the configured credential managers provided credentials
			//	Log.Debug($"None off the configured managers could provide credentials");
			//	if (command == "get")
			//	{
			//		Log.Debug($"Input for get:\n{commandRequest}");
			//		Log.Debug($"Return no credentials");
			//		WriteLine($"username=");
			//		Write($"password=");
			//	}
			//	else
			//	{
			//		// Log.Debug($"Input:\n{commandRequest}");
			//	}
			//}
		}


		private static bool IsCredentialCall(IReadOnlyList<string> args) =>
			args.Count == 3 &&
			args[1] == "--cmg" &&
			(args[2] == "get" || args[2] == "store" || args[2] == "erase");


		private static string ReadCommandResponseText(Process process)
		{
			return process.StandardOutput.ReadToEnd();
		}


		private static void WriteCommandResponseText(string outputText)
		{
			using (StreamWriter outputStream = new StreamWriter(Console.OpenStandardOutput()))
			{
				outputStream.Write(outputText);
				// Log.Debug($"Wrote response to git:\n{outputText}");
			}
		}


		private static void WriteCommandRequestText(string inputText, Process process)
		{
			if (!string.IsNullOrEmpty(inputText))
			{
				process.StandardInput.Write(inputText);
				//Log.Debug($"Wrote request to credential manager:\n{inputText}");
			}
		}


		private static string ReadCommandRequestText()
		{
			StreamReader inputStream = new StreamReader(Console.OpenStandardInput());
			string inputText = inputStream.ReadToEnd();

			return inputText;
		}

		private static void Write(string text)
		{
			// Log.Debug($"Write: {text}");
			Console.Write(text);
		}

		private static void WriteLine(string line)
		{
			// Log.Debug($"Write line: {line}");
			Console.WriteLine(line);
		}

		private Process StartCredentialsManager(string argument)
		{
			string cmd = Quote(CredentialsMgrPath);
			Log.Debug($"{cmd} {argument}");

			Process process = new Process();
			process.StartInfo.FileName = cmd;
			process.StartInfo.Arguments = argument;
			SetProcessOptions(process);

			process.Start();
			return process;
		}


		private static void SetProcessOptions(Process process)
		{
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardInput = true;
			process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
			process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
			process.EnableRaisingEvents = true;
		}

		private static string Quote(string text)
		{
			text = text.Trim();
			text = text.Trim(QuoteChar);
			return $"\"{text}\"";
		}
	}
}