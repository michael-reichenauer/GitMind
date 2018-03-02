using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	internal interface IGitFetch
	{
		Task FetchAsync(CancellationToken ct);
	}
}