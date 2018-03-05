using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitVersion
	{
		Task<string> GetAsync(CancellationToken ct);
	}
}