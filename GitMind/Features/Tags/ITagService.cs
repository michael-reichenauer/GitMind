using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Git;
using GitMind.GitModel.Private;


namespace GitMind.Features.Tags
{
	internal interface ITagService
	{
		void CopyTags(GitRepository repo, MRepository repository);

		Task AddTag(CommitSha commitSha);
	}
}