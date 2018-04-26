﻿using System;
using System.Diagnostics;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Utils;


namespace GitMind.ApplicationHandling.Private
{
	internal class RestartService : IRestartService
	{
		private static readonly char[] QuoteChar = "\"".ToCharArray();


		public bool TriggerRestart(string workingFolder)
		{
			string folder = workingFolder;

			string targetPath = ProgramPaths.GetInstallFilePath();
			string arguments = string.IsNullOrWhiteSpace(folder) ? "/run" : $"/run d:/\"{folder}\"";

			Log.Info($"Restarting: {targetPath} {arguments}");

			return StartProcess(targetPath, arguments);
		}


		private static bool StartProcess(string targetPath, string arguments)
		{
			try
			{
				Process process = new Process();
				process.StartInfo.FileName = Quote(targetPath);
				process.StartInfo.Arguments = arguments;

				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;

				process.Start();
				return true;
			}
			catch (Exception e)
			{
				Log.Exception(e, $"Failed to start {targetPath} {arguments}");
				return false;
			}
		}


		private static string Quote(string text)
		{
			text = text.Trim();
			text = text.Trim(QuoteChar);
			return $"\"{text}\"";
		}
	}
}
