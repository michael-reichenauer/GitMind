using System.Collections.Generic;
using GitMind.Utils;


namespace GitMind.Features.StatusHandling
{
	//public class Status
	//{
	//	public static readonly Status Default =
	//		new Status(new StatusFile[0], new StatusFile[0], false, false, "");

	//	internal Status(
	//		IReadOnlyList<StatusFile> changedFiles,
	//		IReadOnlyList<StatusFile> conflictFiles,
	//		bool isMerging,
	//		bool isFullyMerged,
	//		string mergeMessage)
	//	{
	//		ChangedFiles = changedFiles;
	//		ConflictFiles = conflictFiles;
	//		IsFullyMerged = isFullyMerged;
	//		IsMerging = isMerging;
	//		MergeMessage = mergeMessage;
	//	}

	//	public bool IsOK => ChangedCount == 0;
	//	public int ChangedCount => ChangedFiles.Count;
	//	public int ConflictCount => ConflictFiles.Count;

	//	public IReadOnlyList<StatusFile> ChangedFiles { get; }
	//	public IReadOnlyList<StatusFile> ConflictFiles { get; }

	//	public string MergeMessage { get; }
	//	public bool IsMerging { get; }
	//	public bool IsFullyMerged { get; }


	//	public bool IsSame(Status other)
	//	{
	//		Log.Debug($"Check if same '{this}' and '{other}'");
	//		return
	//			ChangedCount == other.ChangedCount
	//			&& ConflictCount == other.ConflictCount
	//			&& IsMerging == other.IsMerging
	//			&& IsFullyMerged == other.IsFullyMerged;
	//	}

	//	public override string ToString() => 
	//		IsOK ? "OK" : $"{ChangedCount} changes, {ConflictCount} conflicts";
	//}
}