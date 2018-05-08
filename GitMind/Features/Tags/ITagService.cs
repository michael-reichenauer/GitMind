using System.Threading.Tasks;
using GitMind.Common;
using GitMind.GitModel;
using GitMind.GitModel.Private;


namespace GitMind.Features.Tags
{
	internal interface ITagService
	{
		Task CopyTagsAsync(MRepository repository);

		Task AddTagAsync(CommitSha commitSha);

		Task DeleteTagAsync(string tagName);
	}
}