namespace GitMind.Utils.Git
{
	internal class GitLog : IGitLog
	{
		private readonly IGitCmd gitCmd;


		public GitLog(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}
	}
}