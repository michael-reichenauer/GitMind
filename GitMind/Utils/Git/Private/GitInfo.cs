using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git.Private
{
	internal class GitInfo : IGitInfo
	{
		private readonly IGitEnvironmentService gitEnvironmentService;


		public GitInfo(IGitEnvironmentService gitEnvironmentService)
		{
			this.gitEnvironmentService = gitEnvironmentService;
		}


		public string TryGetWorkingFolderRoot(string path) =>
			gitEnvironmentService.TryGetWorkingFolderRoot(path);
	}
}