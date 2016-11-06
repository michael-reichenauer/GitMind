using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.GitModel;


namespace GitMind.Features.Diffing
{
	internal interface IDiffService
	{
		Task ShowDiffAsync(string commitId, string workingFolder);

		Task ShowFileDiffAsync(string workingFolder, string commitId, string name);
		Task ShowDiffRangeAsync(string id1, string id2, string workingFolder);

		Task MergeConflictsAsync(string workingFolder, string id, CommitFile file);
		bool CanMergeConflict(CommitFile file);


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
		IReadOnlyList<string> GetAllTempNames();
		void ShowDiff(string uncommittedId, string workingFolder);
	}
}