using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	internal interface IGitVersion
	{
		Task<string> GetAsync(CancellationToken ct);
	}
}