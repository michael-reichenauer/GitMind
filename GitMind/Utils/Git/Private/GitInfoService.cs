namespace GitMind.Utils.Git.Private
{
	internal class GitInfoService : IGitInfoService
	{
		private readonly IGitEnvironmentService gitEnvironmentService;


		public GitInfoService(IGitEnvironmentService gitEnvironmentService)
		{
			this.gitEnvironmentService = gitEnvironmentService;
		}


		public R<string> GetWorkingFolderRoot(string path)
		{
			string rootPath = gitEnvironmentService.TryGetWorkingFolderRoot(path);

			return !string.IsNullOrEmpty(rootPath) ? R.From(rootPath) : R<string>.NoValue;
		}
	}
}