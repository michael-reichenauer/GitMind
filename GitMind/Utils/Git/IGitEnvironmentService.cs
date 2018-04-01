using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitEnvironmentService
	{
		string GetGitCmdPath();
		Task<string> TryGetWorkingFolderRootAsync(string path, CancellationToken ct);
		Task<string> TryGetGitCorePathAsync(CancellationToken ct);
		Task<string> TryGetGitCmdPathAsync(CancellationToken ct);
		Task<string> TryGetGitVersionAsync(CancellationToken ct);
	}
}