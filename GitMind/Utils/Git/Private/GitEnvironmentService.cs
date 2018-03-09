namespace GitMind.Utils.Git.Private
{
	internal class GitEnvironmentService : IGitEnvironmentService
	{
		private static string GitCmdPath => @"C:\Work Files\MinGit\cmd\git.exe";

		public string GetGitCmdPath() => GitCmdPath;
	}
}