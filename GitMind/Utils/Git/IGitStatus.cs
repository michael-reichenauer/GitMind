using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.Git.Private;


namespace GitMind.Utils.Git
{
	public interface IGitStatus
	{
		Task<R<Status2>> GetStatusAsync(CancellationToken ct);
	}
}