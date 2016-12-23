using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Common;


namespace GitMind.GitModel
{
	internal interface ICommitsFiles
	{
		Task<IEnumerable<CommitFile>> GetAsync(CommitSha commitSha);
	}
}