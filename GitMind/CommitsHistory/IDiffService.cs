using System.Threading.Tasks;


namespace GitMind.CommitsHistory
{
	internal interface IDiffService
	{
		Task ShowDiffAsync(string commitId);

		Task ShowFileDiffAsync(string commitId, string name);
	}
}