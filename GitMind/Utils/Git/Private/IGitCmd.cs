using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git
{
	public interface IGitCmd
	{
		Task<CmdResult2> DoAsync(string args, CancellationToken ct);
	}
}