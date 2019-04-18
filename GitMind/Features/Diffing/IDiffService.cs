using System.Collections.Generic;
using System.Threading.Tasks;
using GitMind.GitModel;


namespace GitMind.Features.Diffing
{
	internal interface IDiffService
	{
		Task ShowDiffAsync(CommitSha commitSha);

		Task ShowFileDiffAsync(CommitSha commitSha, string name);
		Task ShowDiffRangeAsync(CommitSha commitSha1, CommitSha commitSha2);

		Task MergeConflictsAsync(CommitSha commitSha, CommitFile file);
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
		Task ShowPreviewMergeDiffAsync(CommitSha commitSha, CommitSha commitSha2);
	}
}