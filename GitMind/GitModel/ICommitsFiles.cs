using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Utils.Git;


namespace GitMind.GitModel
{
	internal interface ICommitsFiles
	{
		Task<IEnumerable<CommitFile>> GetAsync(CommitSha commitSha, GitStatus2 repositoryStatus);
	}
}