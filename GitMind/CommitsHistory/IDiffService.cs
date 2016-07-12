using System.Threading.Tasks;


namespace GitMind.CommitsHistory
{
	internal interface IDiffService
	{
		Task ShowDiffAsync(string commitId, string workingFolder);

		Task ShowFileDiffAsync(string workingFolder, string commitId, string name);
	}
}