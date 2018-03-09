using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitInfo
	{
		Task<string> GetVersionAsync(CancellationToken ct);
		Task<string> GetGitPathAsync(CancellationToken ct);
	}
}