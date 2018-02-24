using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	internal interface IGitLog
	{
		Task<IReadOnlyList<LogCommit>> GetAsync(CancellationToken ct);
	}
}