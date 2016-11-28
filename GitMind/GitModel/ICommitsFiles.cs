using System.Collections.Generic;
using System.Threading.Tasks;


namespace GitMind.GitModel
{
	internal interface ICommitsFiles
	{
		Task<IEnumerable<CommitFile>> GetAsync(string commitId);
	}
}