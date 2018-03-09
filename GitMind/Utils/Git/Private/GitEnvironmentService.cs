using System.IO;
using GitMind.ApplicationHandling.SettingsHandling;


namespace GitMind.Utils.Git.Private
{
	[SingleInstance]
	internal class GitEnvironmentService : IGitEnvironmentService
	{
		private static readonly string GitVersion = "2.16.2.windows.1";
		private static string GitDefaultCmdPath => @"git.exe";


		public string GetGitCmdPath()
		{
			string gitPath = Path.Combine(ProgramPaths.DataFolderPath, "Git", GitVersion);
			string gitCmdPath = Path.Combine(gitPath, "cmd", "git.exe");

			if (File.Exists(gitCmdPath))
			{
				return gitCmdPath;
			}

			return GitDefaultCmdPath;
		}
	}
}