using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitLog
	{
		Task<R<IReadOnlyList<LogCommit>>> GetAsync(CancellationToken ct);
	}
}