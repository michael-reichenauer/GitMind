using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitStatus
	{
		Task<R<Status2>> GetStatusAsync(CancellationToken ct);
	}
}