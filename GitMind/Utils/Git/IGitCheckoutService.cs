using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitCheckoutService
	{
		Task<R> CheckoutAsync(string name, CancellationToken ct);
		Task<R<bool>> TryCheckoutAsync(string name, CancellationToken ct);
	}
}