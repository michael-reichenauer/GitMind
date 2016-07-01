using System.Collections.Generic;


namespace GitMind.DataModel.Old
{
	internal interface IBranch
	{
		string Name { get; }

		string TrackingName { get; }

		OldCommit LatestLocalCommit { get; }

		OldCommit LatestTrackingCommit { get; }

		OldCommit LatestCommit { get; }

		OldCommit FirstCommit { get; }

		IReadOnlyList<OldCommit> Commits { get; }

		OldBranchBuilder Parent { get; }

		bool IsMultiBranch { get; }

		int LocalAheadCount { get; }

		int RemoteAheadCount { get; }
	}
}