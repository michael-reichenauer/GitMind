using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Git;
using ProtoBuf;


namespace GitMind.GitModel.Private
{
	[ProtoContract]
	public class MCommit
	{
		public static readonly string UncommittedId = GitCommit.UncommittedId;


		[ProtoMember(1)]
		public string Id { get; set; }
		[ProtoMember(2)]
		public string BranchId { get; set; }
		[ProtoMember(3)]
		public string ShortId { get; set; }
		[ProtoMember(4)]
		public string Subject { get; set; }
		[ProtoMember(5)]
		public string Author { get; set; }
		[ProtoMember(6)]
		public DateTime AuthorDate { get; set; }
		[ProtoMember(7)]
		public DateTime CommitDate { get; set; }

		[ProtoMember(8)]
		public List<string> ParentIds { get; set; } = new List<string>();
	
		[ProtoMember(9)]
		public string BranchName { get; set; }
		[ProtoMember(10)]
		public string SpecifiedBranchName { get; set; }

		[ProtoMember(11)]
		public bool IsLocalAheadMarker { get; set; }
		[ProtoMember(12)]
		public bool IsRemoteAheadMarker { get; set; }
		[ProtoMember(13)]
		public string Tags { get; set; }
		[ProtoMember(14)]
		public string Tickets { get; set; }
		[ProtoMember(15)]
		public bool IsVirtual { get; set; }
		[ProtoMember(16)]
		public string BranchTips { get; set; }


		public string SubBranchId { get; set; }
		public string FromSubjectBranchName { get; set; }
		public List<MSubBranch> BranchTipBranches { get; set; } = new List<MSubBranch>();
		
		public bool HasBranchName => !string.IsNullOrEmpty(BranchName);
		public bool HasFirstParent => ParentIds.Count > 0;
		public bool HasSecondParent => ParentIds.Count > 1;
		public bool HasFirstChild => FirstChildIds.Any();
		public bool HasSingleFirstChild => FirstChildIds.Count == 1;

		public MRepository Repository { get; set; }
		public IEnumerable<MCommit> Parents => ParentIds.Select(id => Repository.Commits[id]);
		public IEnumerable<MCommit> Children => Repository.ChildIds(Id).Select(id => Repository.Commits[id]);
		private IList<string> FirstChildIds => Repository.FirstChildIds(Id);
		public IEnumerable<MCommit> FirstChildren => Repository.FirstChildIds(Id).Select(id => Repository.Commits[id]);
		public MBranch Branch => Repository.Branches[BranchId];


		public string FirstParentId => ParentIds.Count > 0 ? ParentIds[0] : null;
		public MCommit FirstParent => ParentIds.Count > 0 ? Repository.Commits[ParentIds[0]] : null;
		public string SecondParentId => ParentIds.Count > 1 ? ParentIds[1] : null;
		public MCommit SecondParent => ParentIds.Count > 1 ? Repository.Commits[ParentIds[1]] : null;
		public bool IsLocalAhead => Branch.IsLocalAndRemote && IsLocalAheadMarker && !IsSynced;
		public bool IsRemoteAhead => Branch.IsLocalAndRemote && IsRemoteAheadMarker && !IsSynced;
		public bool IsSynced => IsLocalAheadMarker && IsRemoteAheadMarker;

		public bool IsUncommitted => Id == UncommittedId;


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