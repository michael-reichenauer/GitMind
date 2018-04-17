using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using GitMind.Common;
using GitMind.Git;


namespace GitMind.GitModel.Private
{
	[DataContract]
	public class MCommit
	{
		private readonly Lazy<GitCommit> gitCommit;

		public MCommit()
		{
			gitCommit = new Lazy<GitCommit>(() => Repository.GitCommits[Id]);
		}

		[DataMember] public CommitId Id { get; set; }
		[DataMember] public string BranchId { get; set; }

		//[DataMember] public string Subject { get; set; }
		//[DataMember]public string Author { get; set; }
		//[DataMember] public DateTime AuthorDate { get; set; }
		//[DataMember] public DateTime CommitDate { get; set; }
		//[DataMember] public List<CommitId> ParentIds { get; set; } = new List<CommitId>();

		public CommitSha Sha => gitCommit.Value.Sha;
		public string Subject => gitCommit.Value.Subject;
		public string Message => gitCommit.Value.Message;
		public string Author => gitCommit.Value.Author;
		public DateTime AuthorDate => gitCommit.Value.AuthorDate;
		public DateTime CommitDate => gitCommit.Value.CommitDate;
		public List<CommitId> ParentIds => gitCommit.Value.ParentIds;
		public BranchName FromSubjectBranchName => gitCommit.Value.BranchNameFromSubject;
		

		[DataMember] public bool IsSet { get; set; }
		[DataMember] public List<CommitId> ChildIds { get; set; } = new List<CommitId>();
		[DataMember] public List<CommitId> FirstChildIds { get; set; } = new List<CommitId>();
		[DataMember] public string BranchName { get; private set; }
		[DataMember] public BranchName SpecifiedBranchName { get; set; }
		[DataMember] public string Tags { get; set; }
		[DataMember] public string Tickets { get; set; }
		[DataMember] public bool IsVirtual { get; set; }
		[DataMember] public string BranchTips { get; set; }
		
		[DataMember] public bool IsLocalAhead { get; set; }
		[DataMember] public bool IsRemoteAhead { get; set; }
		[DataMember] public bool IsCommon { get; set; }


		public CommitId RealCommitId => IsVirtual && Id != CommitId.Uncommitted && Id != CommitId.NoCommits ? FirstParent.Id : Id;
		public CommitSha RealCommitSha => IsVirtual && Id != CommitId.Uncommitted && Id != CommitId.NoCommits ? FirstParent.Sha : Sha;

		public string ShortId => RealCommitSha.ShortSha;

		public string SubBranchId { get; set; }
		
		public List<MSubBranch> BranchTipBranches { get; set; } = new List<MSubBranch>();
		public bool IsMerging { get; set; }
		public bool HasConflicts { get; set; }

		public bool IsLocal { get; set; }
		public bool IsRemote { get; set; }

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


		public CommitId FirstParentId => ParentIds.Count > 0 ? ParentIds[0] : CommitId.None;
		public MCommit FirstParent => ParentIds.Count > 0 ? Repository.Commits[ParentIds[0]] : null;
		public CommitId SecondParentId => ParentIds.Count > 1 ? ParentIds[1] : CommitId.None;
		public MCommit SecondParent => ParentIds.Count > 1 ? Repository.Commits[ParentIds[1]] : null;

		public bool IsUncommitted => Id == CommitId.Uncommitted;
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

		public IEnumerable<MCommit> FirstAncestorsAnSelf()
		{ 
			yield return this;

			foreach (MCommit ancestor in FirstAncestors())
			{				
				yield return ancestor;				
			}
		}


		public IEnumerable<MCommit> CommitAndAncestors(Func<MCommit, bool> predicate)
		{
			if (!predicate(this))
			{
				yield break;
			}

			yield return this;

			foreach (MCommit ancestor in Ancestors(predicate))
			{
				yield return ancestor;
			}
		}

		public IEnumerable<MCommit> Ancestors(Func<MCommit, bool> predicate)
		{
			Stack<MCommit> commits = new Stack<MCommit>();
			Parents
				.Where(predicate)
				.ForEach(child => commits.Push(child));

			while (commits.Any())
			{
				MCommit commit = commits.Pop();
				yield return commit;

				commit.Parents
					.Where(predicate)
					.ForEach(child => commits.Push(child));
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


		public void SetBranchName(BranchName branchName)
		{
			BranchName = branchName;
		}
	}
}