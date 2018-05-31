using System;
using System.Diagnostics;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Utils;


namespace GitMind.ApplicationHandling.Private
{
	internal class StartInstanceService : IStartInstanceService
	{
		private static readonly char[] QuoteChar = "\"".ToCharArray();


		public bool StartInstance(string workingFolder)
		{
			string targetPath = ProgramInfo.GetInstallFilePath();
			string arguments = string.IsNullOrWhiteSpace(workingFolder) ? "/run" : $"/run /d:\"{workingFolder}\"";

			Log.Info($"Restarting: {targetPath} {arguments}");

			return StartProcess(targetPath, arguments);
		}


		public bool OpenOrStartInstance(string workingFolder)
		{
			string targetPath = ProgramInfo.GetInstallFilePath();
			string arguments =  $"/d:\"{workingFolder}\"";

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
