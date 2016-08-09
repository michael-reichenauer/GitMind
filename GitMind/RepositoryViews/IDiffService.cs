using System.Threading.Tasks;
using GitMind.Git;


namespace GitMind.RepositoryViews
{
	internal interface IDiffService
	{
		Task ShowDiffAsync(string commitId, string workingFolder);

		Task ShowFileDiffAsync(string workingFolder, string commitId, string name);
		Task ShowDiffRangeAsync(string id1, string id2, string workingFolder);

		Task MergeConflictsAsync(string workingFolder, string id, string path, GitConflict gitConflict);
		Task ResolveAsync(string workingFolder, string path);
	}
}