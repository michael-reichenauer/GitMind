using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.GitModel;


namespace GitMind.Features.Diffing
{
	internal interface IDiffService
	{
		Task ShowDiffAsync(CommitSha commitId);

		Task ShowFileDiffAsync(CommitSha commitId, string name);
		Task ShowDiffRangeAsync(CommitSha id1, CommitSha id2);

		Task MergeConflictsAsync(CommitSha id, CommitFile file);
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
		void ShowDiff(CommitSha uncommittedId);
	}
}