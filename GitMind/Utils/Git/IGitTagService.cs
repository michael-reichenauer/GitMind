using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git
{
	public interface IGitTagService
	{
		Task<R<IReadOnlyList<GitTag>>> GetAllTagsAsync(CancellationToken ct);

		Task<R<GitTag>> AddTagAsync(string sha, string tagName, CancellationToken ct);
		Task<R> DeleteTagAsync(string tagName, CancellationToken ct);
	}
}