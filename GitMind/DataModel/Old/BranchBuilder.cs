using System.Collections.Generic;
using System.Linq;


namespace GitMind.DataModel.Old
{
	internal class BranchBuilder : IBranch
	{
		public static readonly BranchBuilder None = new BranchBuilder("", null, OldCommit.None, OldCommit.None);

		public BranchBuilder(
			string name,
			string trackingName,
			OldCommit latestLocalCommit,
			OldCommit latestTrackingCommit)
		{
			Name = name;
			ShowName = name;
			TrackingName = trackingName;
			LatestLocalCommit = latestLocalCommit;
			LatestTrackingCommit = latestTrackingCommit;

			Parent = None;
		}


		public string Name { get; }
		public string ShowName { get; set; }

		public string TrackingName { get; }
		public OldCommit LatestLocalCommit { get; set; }
		public OldCommit LatestTrackingCommit { get; set; }

		public OldCommit LatestCommit => Commits.Any() ? Commits.First() : LatestLocalCommit;
	
		public OldCommit FirstCommit => Commits.Any() ? Commits.Last() : LatestLocalCommit;

		public IReadOnlyList<OldCommit> Commits => CommitsBuilder;

		public List<OldCommit> CommitsBuilder { get; } = new List<OldCommit>();

		public BranchBuilder Parent { get; set; }

		public bool IsMultiBranch { get; set; }

		public List<BranchBuilder> MultiBranches { get; } = new List<BranchBuilder>();

		public int LocalAheadCount { get; set; }
		public int RemoteAheadCount { get; set; }


		public override string ToString() => Name;
	}
}