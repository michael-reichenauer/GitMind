using System.Collections.Generic;
using System.Linq;


namespace GitMind.DataModel.Old
{
	internal class BranchBuilder : IBranch
	{
		public static readonly BranchBuilder None = new BranchBuilder("", null, Commit.None, Commit.None);

		public BranchBuilder(
			string name,
			string trackingName,
			Commit latestLocalCommit,
			Commit latestTrackingCommit)
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
		public Commit LatestLocalCommit { get; set; }
		public Commit LatestTrackingCommit { get; set; }

		public Commit LatestCommit => Commits.Any() ? Commits.First() : LatestLocalCommit;
	
		public Commit FirstCommit => Commits.Any() ? Commits.Last() : LatestLocalCommit;

		public IReadOnlyList<Commit> Commits => CommitsBuilder;

		public List<Commit> CommitsBuilder { get; } = new List<Commit>();

		public BranchBuilder Parent { get; set; }

		public bool IsMultiBranch { get; set; }

		public List<BranchBuilder> MultiBranches { get; } = new List<BranchBuilder>();

		public int LocalAheadCount { get; set; }
		public int RemoteAheadCount { get; set; }


		public override string ToString() => Name;
	}
}