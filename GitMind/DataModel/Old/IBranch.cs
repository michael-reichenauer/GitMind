using System.Collections.Generic;


namespace GitMind.DataModel.Old
{
	internal interface IBranch
	{
		string Name { get; }

		string TrackingName { get; }

		Commit LatestLocalCommit { get; }

		Commit LatestTrackingCommit { get; }

		Commit LatestCommit { get; }

		Commit FirstCommit { get; }

		IReadOnlyList<Commit> Commits { get; }

		BranchBuilder Parent { get; }

		bool IsMultiBranch { get; }

		int LocalAheadCount { get; }

		int RemoteAheadCount { get; }
	}
}