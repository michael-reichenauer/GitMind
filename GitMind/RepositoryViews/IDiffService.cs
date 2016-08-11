using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Git;
using GitMind.GitModel;


namespace GitMind.RepositoryViews
{
	internal interface IDiffService
	{
		Task ShowDiffAsync(string commitId, string workingFolder);

		Task ShowFileDiffAsync(string workingFolder, string commitId, string name);
		Task ShowDiffRangeAsync(string id1, string id2, string workingFolder);

		Task MergeConflictsAsync(string workingFolder, string id, CommitFile file);
		bool CanMergeConflict(CommitFile file);

		Task ResolveAsync(string workingFolder, CommitFile path);
		bool CanResolve(string workingFolder, CommitFile file);
		Task UseYoursAsync(string workingFolder, CommitFile file);
		bool CanUseYours(CommitFile file);
		Task UseTheirsAsync(string workingFolder, CommitFile file);
		bool CanUseTheirs(CommitFile file);
		Task UseBaseAsync(string workingFolder, CommitFile file);
		bool CanUseBase(string workingFolder, CommitFile file);
		Task DeleteAsync(string workingFolder, CommitFile file);
		bool CanDelete(string workingFolder, CommitFile file);
		Task ShowYourDiffAsync(string workingFolder, CommitFile file);
		Task ShowTheirDiffAsync(string workingFolder, CommitFile file);
		bool IsUseYours(string workingFolder, CommitFile file);
		bool IsUseTheirs(string workingFolder, CommitFile file);
		bool IsUseBase(string workingFolder, CommitFile file);
		bool IsDeleted(string workingFolder, CommitFile file);
		bool IsMerged(string workingFolder, CommitFile file);
		IReadOnlyList<string> GetAllTempNames();
	}
}