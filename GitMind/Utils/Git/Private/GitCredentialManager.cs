using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;


namespace GitMind.Utils.Git.Private
{
	internal class GitCredentialManager : IGitCredentialManager
	{
		private static readonly string CredentialsMgrPath =
			@"C:\Work Files\MinGit\mingw64\libexec\git-core\git-credential-manager.exe";

		private readonly IGitConfig gitConfig;


		public GitCredentialManager(IGitConfig gitConfig)
		{
			this.gitConfig = gitConfig;
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
			string command = args[1];
			Log.Debug($"Command: {command}");

			string commandRequest = ReadCommandRequestText();
			// Log.Debug($"Input:\n{commandRequest}");


			gitConfig.TryGet("credential.helper", out GitSetting helper);
			if (helper == null || helper.Values.All(v => v == "!GitMind.exe"))
			{
				if (command == "get")
				{
					WriteLine($"path=");
					WriteLine($"username=");
					WriteLine($"password=");
				}

				return;
			}

			Log.Debug($"Helper: '{helper}'");

			Process process = StartCredentialsManager(command);

			WriteCommandRequestText(commandRequest, process);

			if (command == "get")
			{
				string outputText = ReadCommandResponseText(process);
				// Log.Debug($"Output:\n{outputText}");

				WriteCommandResponseText(outputText);
			}

			process.WaitForExit();
			Log.Debug($"Exit code {process.ExitCode}");
			process.Close();
		}


		private static bool IsCredentialCall(IReadOnlyList<string> args) =>
			args.Count == 2 && (args[1] == "get" || args[1] == "store" || args[1] == "erase");


		private static string ReadCommandResponseText(Process process)
		{
			return process.StandardOutput.ReadToEnd();
		}


		private static void WriteCommandResponseText(string outputText)
		{
			using (StreamWriter outputStream = new StreamWriter(Console.OpenStandardOutput()))
			{
				outputStream.Write(outputText);
			}
		}


		private static void WriteCommandRequestText(string inputText, Process process)
		{
			if (!string.IsNullOrEmpty(inputText))
			{
				process.StandardInput.Write(inputText);
				Log.Debug("Wrote inout text to git-credential-manager");
			}
		}


		private static string ReadCommandRequestText()
		{
			StreamReader inputStream = new StreamReader(Console.OpenStandardInput());
			string inputText = inputStream.ReadToEnd();
			if (!string.IsNullOrEmpty(inputText) && !inputText.EndsWith("\n\n"))
			{
				// The credentials manager expects empty last line
				inputText += "\n";
			}

			return inputText;
		}

		private static void WriteLine(string line)
		{
			// Log.Debug($"Write line: {line}");
			Console.WriteLine(line);
		}

		private static Process StartCredentialsManager(string argument)
		{
			Process process = new Process();
			process.StartInfo.FileName = CredentialsMgrPath;
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
	}
}