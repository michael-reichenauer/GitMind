using System.Threading.Tasks;


namespace GitMind.RepositoryViews
{
	internal interface IDiffService
	{
		Task ShowDiffAsync(string commitId, string workingFolder);

		Task ShowFileDiffAsync(string workingFolder, string commitId, string name);
	}
}