using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	internal interface IGitPush
	{
		Task PushAsync(CancellationToken ct);
	}
}