using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	public interface IGitCmd
	{
		Task<CmdResult2> DoAsync(string args, CancellationToken ct);
	}
}