using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	public class MCommit
	{
		public static readonly string UncommittedId = GitCommit.UncommittedId;

		// Serialized start -------------------

		public int IndexId { get; set; }
		public string BranchId { get; set; }
		public string ShortId { get; set; }
		public string Subject { get; set; }
		public string Author { get; set; }
		public DateTime AuthorDate { get; set; }
		public DateTime CommitDate { get; set; }

		public List<int> ParentIds { get; set; } = new List<int>();
		public List<int> ChildIds { get; set; } = new List<int>();
		public List<int> FirstChildIds { get; set; } = new List<int>();

		public BranchName BranchName { get; set; }
		public BranchName SpecifiedBranchName { get; set; }

		public string Tags { get; set; }
		public string Tickets { get; set; }
		public bool IsVirtual { get; set; }
		public string BranchTips { get; set; }
		public string CommitId { get; set; }
		public bool IsLocalAhead { get; set; }
		public bool IsRemoteAhead { get; set; }
		public bool IsCommon { get; set; }

		// Serialized Done ---------------------


		public string SubBranchId { get; set; }
		public BranchName FromSubjectBranchName { get; set; }
		public List<MSubBranch> BranchTipBranches { get; set; } = new List<MSubBranch>();
		public bool IsMerging { get; set; }
		public bool HasConflicts { get; set; }

		public bool HasBranchName => BranchName != null;
		public bool HasFirstParent => ParentIds.Count > 0;
		public bool HasSecondParent => ParentIds.Count > 1;
		public bool HasFirstChild => FirstChildIds.Any();
		public bool HasSingleFirstChild => FirstChildIds.Count == 1;

		public MRepository Repository { get; set; }
		public IEnumerable<MCommit> Parents => ParentIds.Select(id => Repository.Commits[id]);

		public IEnumerable<MCommit> Children => ChildIds.Select(id => Repository.Commits[id]);
		
		public IEnumerable<MCommit> FirstChildren => FirstChildIds.Select(id => Repository.Commits[id]);
		public MBranch Branch => Repository.Branches[BranchId];


		public int FirstParentId => ParentIds.Count > 0 ? ParentIds[0] : -1;
		public MCommit FirstParent => ParentIds.Count > 0 ? Repository.Commits[ParentIds[0]] : null;
		public int SecondParentId => ParentIds.Count > 1 ? ParentIds[1] : -1;
		public MCommit SecondParent => ParentIds.Count > 1 ? Repository.Commits[ParentIds[1]] : null;

		public bool IsUncommitted => CommitId == UncommittedId;
		public BranchName CommitBranchName { get; set; }


		public IEnumerable<MCommit> FirstAncestors()
		{
			MCommit current = FirstParent;
			while (current != null)
			{
				yield return current;
				current = current.FirstParent;
			}
		}

		public IEnumerable<MCommit> CommitAndFirstAncestors()
		{
			yield return this;

			MCommit current = FirstParent;
			while (current != null)
			{
				yield return current;
				current = current.FirstParent;
			}
		}

		public override string ToString() => $"{ShortId} {AuthorDate} ({ParentIds.Count}) {Subject} ({CommitDate})";
	}
}