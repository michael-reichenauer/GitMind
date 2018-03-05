using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	internal interface IGitConfig
	{
		Task<IReadOnlyDictionary<string, string>> GetAsync(CancellationToken ct);
	}
}