using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitPush
	{
		Task<bool> PushAsync(CancellationToken ct);
	}
}