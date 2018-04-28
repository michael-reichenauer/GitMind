using System.Collections.Generic;
using System.Threading.Tasks;


namespace GitMind.RepositoryViews.Open
{
	public interface IOpenModelService
	{
		Task OpenModelAsync();

		Task OpenOtherModelAsync(string modelFilePath);

		Task TryModelAsync(string modelFilePath);


		Task OpenModelAsync(IReadOnlyList<string> modelFilePaths);
	}
}