using System.Collections.Generic;
using System.Linq;
using GitMind.Utils;


namespace GitMind.DataModel.Private
{
	internal class XModel
	{
		public KeyedList<string, XCommit> Commits = new KeyedList<string, XCommit>(c => c.Id);
		public KeyedList<string, SubBranch> SubBranches = new KeyedList<string, SubBranch>(b => b.Id);	
		public KeyedList<string, XBranch> Branches = new KeyedList<string, XBranch>(b => b.Id);

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

		public string ParentBranchId { get; set; }

		public bool IsMultiBranch { get; set; }
		public bool IsActive { get; set; }
		public bool IsAnonymous { get; set; }


		public List<SubBranch> SubBranches { get; } = new List<SubBranch>();
		public List<XCommit> Commits { get; } = new List<XCommit>();

		public List<XBranch> ChildBranches { get; } = new List<XBranch>();

		//public int RemoteAheadCount { get; set; }
		//public int LocalAheadCount { get; set; }
		//public string TrackingName { get; set; }
		//public string LastestLocalCommitId { get; set; }
		//public string LastestTrackingCommitId { get; set; }


		public XCommit FirstCommit => XModel.Commits[FirstCommitId];
		public XCommit LatestCommit => XModel.Commits[LatestCommitId];
		public XCommit ParentCommit => XModel.Commits[ParentCommitId];
		public XBranch ParentBranch => XModel.Branches[ParentBranchId];

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


		public XCommit FirstCommit => XModel.Commits[FirstCommitId];
		public XCommit LatestCommit => XModel.Commits[LatestCommitId];
		public XCommit ParentCommit => XModel.Commits[ParentCommitId];

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
		public IEnumerable<XCommit> Parents => ParentIds.Select(id => xModel.Commits[id]);
		public IEnumerable<XCommit> Children => ChildIds.Select(id => xModel.Commits[id]);
		public IEnumerable<XCommit> FirstChildren => FirstChildIds.Select(id => xModel.Commits[id]);

		public string branchName;
		public string BranchName
		{
			get { return branchName; }
			set
			{
				branchName = value;

				//if (ShortId == "afe62f")
				//{
				//	Log.Warn($"Setting branch name {branchName} != '{BranchNameFromSubject}' from subject");
				//}
			}
		}
		public string SubBranchId { get; set; }

		public string BranchNameSpecified { get; set; }
		public string BranchNameFromSubject { get; set; }
		public string MergeSourceBranchNameFromSubject { get; set; }
		public string MergeTargetBranchNameFromSubject { get; set; }
		public string Subject { get; set; }
		public string Author { get; set; }
		public string AuthorDate { get; set; }
		public string CommitDate { get; set; }

		public string FirstParentId => ParentIds.Count > 0 ? ParentIds[0] : null;
		public XCommit FirstParent => ParentIds.Count > 0 ? xModel.Commits[ParentIds[0]] : null;
		public string SecondParentId => ParentIds.Count > 1 ? ParentIds[1] : null;
		public XCommit SecondParent => ParentIds.Count > 1 ? xModel.Commits[ParentIds[1]] : null;



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
