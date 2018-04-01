using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitInfo
	{
		Task<string> TryGetWorkingFolderRootAsync(string path, CancellationToken ct);
	}
}