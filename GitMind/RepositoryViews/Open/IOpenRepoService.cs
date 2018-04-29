using System.Collections.Generic;
using System.Threading.Tasks;


namespace GitMind.RepositoryViews.Open
{
	public interface IOpenRepoService
	{
		Task OpenRepoAsync();

		Task OpenOtherRepoAsync(string modelFilePath);

		Task TryOpenRepoAsync(string modelFilePath);


		Task OpenRepoAsync(IReadOnlyList<string> modelFilePaths);
		Task CloneRepoAsync();
		Task InitRepoAsync();
	}
}