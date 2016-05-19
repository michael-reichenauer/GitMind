using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


namespace GitMind.DataModel.Private
{
	internal class Keyed<TKey, TValue> : KeyedCollection<TKey, TValue>
	{
		private readonly Func<TValue, TKey> getKeyForItem;

		public Keyed(Func<TValue, TKey> getKeyForItem)
		{
			this.getKeyForItem = getKeyForItem;
		}


		protected override TKey GetKeyForItem(TValue item)
		{
			return getKeyForItem(item);
		}
	}

	internal class XModel
	{
		//public Keyed<string, XCommit> Commits = new Keyed<string, XCommit>(c => c.Id);

		public List<XCommit> Commits { get; } = new List<XCommit>();
		public Dictionary<string, XCommit> CommitById { get; set; } = new Dictionary<string, XCommit>();

		public List<SubBranch> SubBranches { get; } = new List<SubBranch>();
		public Dictionary<string, SubBranch> SubBranchById { get; } = new Dictionary<string, SubBranch>();

		public List<XBranch> Branches { get; } = new List<XBranch>();
		public Dictionary<string, XBranch> BranchById { get; } = new Dictionary<string, XBranch>();

		public string CurrentCommitId { get; set; }
		public string CurrentBranchId { get; set; }
	}


	internal class XBranch
	{
		public XBranch(XModel xModel)
		{
			XModel = xModel;
		}


		public XModel XModel { get; }

		public string Id { get; set; }
		public string Name { get; set; }

		public string LatestCommitId { get; set; }
		public string FirstCommitId { get; set; }
		public string ParentCommitId { get; set; }

		public bool IsMultiBranch { get; set; }
		public bool IsActive { get; set; }
		public bool IsAnonymous { get; set; }


		public List<SubBranch> SubBranches { get; } = new List<SubBranch>();
		public List<XCommit> Commits { get; } = new List<XCommit>();

		//public int RemoteAheadCount { get; set; }
		//public int LocalAheadCount { get; set; }
		//public string TrackingName { get; set; }
		//public string LastestLocalCommitId { get; set; }
		//public string LastestTrackingCommitId { get; set; }


		public XCommit FirstCommit => XModel.CommitById[FirstCommitId];
		public XCommit LatestCommit => XModel.CommitById[LatestCommitId];
		public XCommit ParentCommit => XModel.CommitById[ParentCommitId];

		public override string ToString() => $"{Name}";
	}



	internal class SubBranch
	{
		public SubBranch(XModel xModel)
		{
			XModel = xModel;
		}


		public XModel XModel { get; }

		public string Id { get; set; }
		public string BranchId { get; set; }

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


		public XCommit FirstCommit => XModel.CommitById[FirstCommitId];
		public XCommit LatestCommit => XModel.CommitById[LatestCommitId];
		public XCommit ParentCommit => XModel.CommitById[ParentCommitId];

		public override string ToString() => $"{Name} ({IsRemote})";
	}


	internal class XCommit
	{
		private readonly XModel xModel;

		public XCommit(XModel xModel)
		{
			this.xModel = xModel;
		}


		public string Id { get; set; }
		public string BranchId { get; set; }
		public string ShortId { get; set; }
		public List<string> ParentIds { get; set; } = new List<string>();
		public List<string> ChildIds { get; set; } = new List<string>();
		public List<string> FirstChildIds { get; set; } = new List<string>();

		public bool HasBranchName => !string.IsNullOrEmpty(BranchName);
		public bool HasFirstParent => ParentIds.Count > 0;
		public bool HasSecondParent => ParentIds.Count > 1;
		public bool HasSingleFirstChild => ChildIds.Count == 1;
		public IEnumerable<XCommit> Parents => ParentIds.Select(id => xModel.CommitById[id]);
		public IEnumerable<XCommit> Children => ChildIds.Select(id => xModel.CommitById[id]);
		public IEnumerable<XCommit> FirstChildren => FirstChildIds.Select(id => xModel.CommitById[id]);

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
		public XCommit FirstParent => ParentIds.Count > 0 ? xModel.CommitById[ParentIds[0]] : null;
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
