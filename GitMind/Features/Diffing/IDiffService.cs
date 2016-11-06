using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.GitModel;


namespace GitMind.Features.Diffing
{
	internal interface IDiffService
	{
		Task ShowDiffAsync(string commitId);

		Task ShowFileDiffAsync(string commitId, string name);
		Task ShowDiffRangeAsync(string id1, string id2);

		Task MergeConflictsAsync(string id, CommitFile file);
		bool CanMergeConflict(CommitFile file);


		Task UseYoursAsync(CommitFile file);
		bool CanUseYours(CommitFile file);
		Task UseTheirsAsync(CommitFile file);
		bool CanUseTheirs(CommitFile file);
		Task UseBaseAsync(CommitFile file);
		bool CanUseBase(CommitFile file);
		Task DeleteAsync(CommitFile file);
		bool CanDelete(CommitFile file);
		Task ShowYourDiffAsync(CommitFile file);
		Task ShowTheirDiffAsync(CommitFile file);
		IReadOnlyList<string> GetAllTempNames();
		void ShowDiff(string uncommittedId);
	}
}