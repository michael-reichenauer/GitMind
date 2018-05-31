using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Utils.Git;


namespace GitMind.GitModel
{
	internal interface ICommitsDetailsService
	{
		Task<CommitDetails> GetAsync(CommitSha commitSha, GitStatus repositoryStatus);
	}
}