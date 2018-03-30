using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitInfo
	{
		Task<string> TryGetGitVersionAsync(CancellationToken ct);
		Task<string> TryGetGitPathAsync(CancellationToken ct);
		Task<string> TryGetWorkingFolderRootAsync(string path, CancellationToken ct);
	}
}