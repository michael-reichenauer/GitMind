using System;
using System.Collections.Generic;
using System.Linq;


namespace GitMind.DataModel.Private
{
	internal class XModel
	{
		public List<XCommit> AllCommits { get; } = new List<XCommit>();
		public Dictionary<string, XCommit> Commit { get; set; } = new Dictionary<string, XCommit>();

		public List<XBranch> AllBranches { get;  } = new List<XBranch>();
		public Dictionary<string, XBranch> IdToBranch { get; } = new Dictionary<string, XBranch>();

		public List<XBranchCommits> BranchCommits { get; } = new List<XBranchCommits>();

		public string CurrentCommitId { get; set; }
		public string CurrentBranchId { get; set; }
	}


	internal class XBranchCommits
	{
		private readonly XModel xModel;

		public XBranchCommits(XModel xModel)
		{
			this.xModel = xModel;
		}

		public string Id { get; set; }
		public string Name { get; set; }

		public string LatestCommitId { get; set; }
		public string FirstCommitId { get; set; }
		public string ParentCommitId { get; set; }

		public bool IsMultiBranch { get; set; }
		public bool IsActive { get; set; }
		public bool IsAnonymous { get; set; }


		public List<XBranch> Branches { get; } = new List<XBranch>();
		public List<XCommit> Commits { get; } = new List<XCommit>();

		//public int RemoteAheadCount { get; set; }
		//public int LocalAheadCount { get; set; }
		//public string TrackingName { get; set; }
		//public string LastestLocalCommitId { get; set; }
		//public string LastestTrackingCommitId { get; set; }


		public XCommit FirstCommit => xModel.Commit[FirstCommitId];
		public XCommit LatestCommit => xModel.Commit[LatestCommitId];
		public XCommit ParentCommit => xModel.Commit[ParentCommitId];

		public override string ToString() => $"{Name}";
	}



	internal class XBranch
	{
		private readonly XModel xModel;


		public XBranch(XModel xModel)
		{
			this.xModel = xModel;
		}

		public string Id { get; set; }
		public string Name { get; set; }
	
		public string LatestCommitId { get; set; }
		public string FirstCommitId { get; set; }
		public string ParentCommitId { get; set; }

		public bool IsMultiBranch { get; set; }
		public bool IsActive { get; set; }
		public bool IsAnonymous { get; set; }
		public bool IsRemote { get; set; }

		//public int RemoteAheadCount { get; set; }
		//public int LocalAheadCount { get; set; }
		//public string TrackingName { get; set; }
		//public string LastestLocalCommitId { get; set; }
		//public string LastestTrackingCommitId { get; set; }


		public XCommit FirstCommit => xModel.Commit[FirstCommitId];
		public XCommit LatestCommit => xModel.Commit[LatestCommitId];
		public XCommit ParentCommit => xModel.Commit[ParentCommitId];

		public override string ToString() =>$"{Name} ({IsRemote})";
	}


	internal class XCommit
	{
		private readonly XModel xModel;

		public XCommit(XModel xModel)
		{
			this.xModel = xModel;
		}


		public string Id { get; set; }
		public string ShortId { get; set; }
		public List<string> ParentIds{ get; set; } = new List<string>();
		public List<string> ChildIds { get; set; } = new List<string>();
		public List<string> FirstChildIds { get; set; } = new List<string>();

		public bool HasBranchName => !string.IsNullOrEmpty(BranchName);
		public bool HasFirstParent => ParentIds.Count > 0;
		public bool HasSecondParent => ParentIds.Count > 1;
		public bool HasSingleFirstChild => ChildIds.Count == 1;
		public IEnumerable<XCommit> Parents => ParentIds.Select(id => xModel.Commit[id]);
		public IEnumerable<XCommit> Children => ChildIds.Select(id => xModel.Commit[id]);
		public IEnumerable<XCommit> FirstChildren => FirstChildIds.Select(id => xModel.Commit[id]);

		public string BranchName { get; set; }
		public string BranchNameSpecified { get; set; }
		public string BranchNameFromSubject { get; set; }
		public string MergeSourceBranchNameFromSubject { get; set; }
		public string MergeTargetBranchNameFromSubject { get; set; }
		public string Subject { get; set; }
		public string Author { get; set; }
		public string AuthorDate { get; set; }
		public string CommitDate { get; set; }

		public string FirstParentId => ParentIds.Count > 0 ? ParentIds[0] : null;
		public XCommit FirstParent => ParentIds.Count > 0 ? xModel.Commit[ParentIds[0]] : null;
		public string SecondParentId => ParentIds.Count > 1 ? ParentIds[1] : null;


		public IEnumerable<XCommit> FirstAncestors()
		{
			XCommit current = FirstParent;
			while (current != null)
			{
				yield return current;
				current = current.FirstParent;			
			}
		}

		public override string ToString() => $"{ShortId} {AuthorDate} ({ParentIds.Count}) {Subject} ({CommitDate})";
	}
}
