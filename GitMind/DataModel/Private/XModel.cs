using System;
using System.Collections.Generic;


namespace GitMind.DataModel.Private
{
	internal class XModel
	{
		public List<XCommit> AllCommits { get; set; } = new List<XCommit>();
		public Dictionary<string, XCommit> Commit { get; set; } = new Dictionary<string, XCommit>();

		public List<XBranch> AllBranches { get; set; } = new List<XBranch>();
		public Dictionary<string, XBranch> IdToBranch { get; set; } = new Dictionary<string, XBranch>();

		public string CurrentCommitId { get; set; }
		public string CurrentBranchId { get; set; }
	}


	internal class XBranch
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string TrackingName { get; set; }
		public string LastestLocalCommitId { get; set; }
		public string LastestTrackingCommitId { get; set; }
		public string ParentId { get; set; }
		public bool IsMultiBranch { get; set; }
		public int LocalAheadCount { get; set; }
		public int RemoteAheadCount { get; set; }
		public bool IsActive { get; set; }

		public override string ToString() => Name;
	}


	internal class XCommit
	{
		public string Id { get; set; }
		public string ShortId { get; set; }
		public List<string> ParentIds{ get; set; } = new List<string>();
		public List<string> ChildIds { get; set; } = new List<string>();
		public string BranchName { get; set; }
		public string BranchNameSpecified { get; set; }
		public string BranchNameFromSubject { get; set; }
		public string SourceBranchNameFromSubject { get; set; }
		public string TargetBranchNameFromSubject { get; set; }
		public string Subject { get; set; }
		public string Author { get; set; }
		public string AuthorDate { get; set; }
		public string CommitDate { get; set; }

		public string FirstParentId => ParentIds.Count > 0 ? ParentIds[0] : null;
		public string SecondParentId => ParentIds.Count > 1 ? ParentIds[1] : null;

		public override string ToString() => $"{ShortId} {CommitDate} ({ParentIds.Count}) {Subject}";
	}
}
