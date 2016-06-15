using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using ProtoBuf;


namespace GitMind.GitModel.Private
{
	[DataContract, ProtoContract]
	public class MCommit
	{
		public MCommit()
		{
			ParentIds = new List<string>();
			ChildIds = new List<string>();
			FirstChildIds = new List<string>();
		}

		[DataMember, ProtoMember(1)]
		public string Id { get; set; }
		[DataMember, ProtoMember(2)]
		public string BranchId { get; set; }
		[DataMember, ProtoMember(3)]
		public string ShortId { get; set; }
		[DataMember, ProtoMember(4)]
		public string Subject { get; set; }
		[DataMember, ProtoMember(5)]
		public string Author { get; set; }
		[DataMember, ProtoMember(6)]
		public DateTime AuthorDate { get; set; }
		[DataMember, ProtoMember(7)]
		public DateTime CommitDate { get; set; }

		[DataMember, ProtoMember(8)]
		public List<string> ParentIds { get; set; }
		[DataMember, ProtoMember(9)]
		public List<string> ChildIds { get; set; }
		[DataMember, ProtoMember(10)]
		public List<string> FirstChildIds { get; set; } 

		[DataMember, ProtoMember(11)]
		public string BranchXName { get; set; }
		[DataMember, ProtoMember(12)]
		public string BranchNameSpecified { get; set; }
		[DataMember, ProtoMember(13)]
		public string BranchNameFromSubject { get; set; }
		[DataMember, ProtoMember(14)]
		public string MergeSourceBranchNameFromSubject { get; set; }
		[DataMember, ProtoMember(15)]
		public string MergeTargetBranchNameFromSubject { get; set; }
		[DataMember, ProtoMember(16)]
		public string SubBranchId { get; set; }
		[DataMember, ProtoMember(17)]
		public bool IsLocalAheadMarker { get; set; }
		[DataMember, ProtoMember(18)]
		public bool IsRemoteAheadMarker { get; set; }


		public bool HasBranchName => !string.IsNullOrEmpty(BranchXName);
		public bool HasFirstParent => ParentIds.Count > 0;
		public bool HasSecondParent => ParentIds.Count > 1;
		public bool HasSingleFirstChild => ChildIds.Count == 1;

		public MRepository Repository { get; set; }
		public IEnumerable<MCommit> Parents => ParentIds.Select(id => Repository.Commits[id]);
		public IEnumerable<MCommit> Children => ChildIds.Select(id => Repository.Commits[id]);
		public IEnumerable<MCommit> FirstChildren => FirstChildIds.Select(id => Repository.Commits[id]);
		public MBranch Branch => Repository.Branches[BranchId];

		//public string BranchName
		//{
		//	get { return branchName; }
		//	set
		//	{
		//		if (ShortId == "c336d1")
		//		{

		//		}
		//		branchName = value;
		//		if (branchName != null && BranchNameFromSubject != null 
		//			&& branchName != BranchNameFromSubject
		//			&& -1 == BranchNameFromSubject.IndexOf("trunk", StringComparison.OrdinalIgnoreCase))
		//		{
		//			//Log.Warn($"Setting branch name {branchName} != '{BranchNameFromSubject}' from subject for {this}");
		//		}
		//		//if (ShortId == "afe62f")
		//		//{
		//		//	Log.Warn($"Setting branch name {branchName} != '{BranchNameFromSubject}' from subject");
		//		//}
		//	}
		//}



		public string FirstParentId => ParentIds.Count > 0 ? ParentIds[0] : null;
		public MCommit FirstParent => ParentIds.Count > 0 ? Repository.Commits[ParentIds[0]] : null;
		public string SecondParentId => ParentIds.Count > 1 ? ParentIds[1] : null;
		public MCommit SecondParent => ParentIds.Count > 1 ? Repository.Commits[ParentIds[1]] : null;
		public bool IsLocalAhead => Branch.IsLocalAndRemote && IsLocalAheadMarker && !IsSynced;
		public bool IsRemoteAhead => Branch.IsLocalAndRemote && IsRemoteAheadMarker && !IsSynced;
		public bool IsSynced => IsLocalAheadMarker && IsRemoteAheadMarker;

		public IEnumerable<MCommit> FirstAncestors()
		{
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