using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.GitModel
{
	internal class Commit : Equatable<Commit>
	{
		private readonly IReadOnlyList<CommitId> parentIds;
		private readonly IReadOnlyList<CommitId> childIds;
		private readonly string branchId;

		public Commit(
			Repository repository, 
			CommitId id,
			CommitId commitId, 
			CommitSha commitSha,
			string subject,
			string author, 
			DateTime authorDate, 
			DateTime commitDate,
			string tags, 
			string tickets, 
			string branchTips, 
			IReadOnlyList<CommitId> parentIds, 
			IReadOnlyList<CommitId> childIds, 
			string branchId,
			BranchName specifiedBranchName,
			BranchName commitBranchName,
			bool isLocalAhead, 
			bool isRemoteAhead,
			bool isCommon,
			bool isUncommitted, 
			bool isVirtual, 
			bool hasConflicts, 
			bool isMerging,
			bool hasFirstChild)
		{
			this.Repository = repository;
			this.parentIds = parentIds;
			this.childIds = childIds;
			this.branchId = branchId;
			Id = id;
			CommitId = commitId;
			CommitSha = commitSha;
			Subject = subject;
			Author = author;
			AuthorDate = authorDate;			
			AuthorDateText = authorDate.ToShortDateString() + " " + authorDate.ToShortTimeString();
			CommitDate = commitDate;
			Tags = tags;
			Tickets = tickets;
			BranchTips = branchTips;
			SpecifiedBranchName = specifiedBranchName;
			CommitBranchName = commitBranchName;
			IsLocalAhead = isLocalAhead;
			IsRemoteAhead = isRemoteAhead;
			IsCommon = isCommon;
			IsUncommitted = isUncommitted;
			IsVirtual = isVirtual;
			HasConflicts = hasConflicts;
			IsMerging = isMerging;
			HasFirstChild = hasFirstChild;
		}


		public CommitId Id { get; }
		public CommitId CommitId { get; }
		public CommitSha CommitSha { get; }
		public string Subject { get; }
		public string Author { get; }
		public DateTime AuthorDate { get; }
		public string AuthorDateText { get; }
		public DateTime CommitDate { get; }
		public string Tags { get; }
		public string Tickets { get; }
		public string BranchTips { get; }
		public BranchName SpecifiedBranchName { get; }
		public BranchName CommitBranchName { get; }
		public bool IsLocalAhead { get; }
		public bool IsRemoteAhead { get; }
		public bool IsCommon { get; }
		public bool IsUncommitted { get; }
		public bool IsVirtual { get; }
		public bool HasConflicts { get; }
		public bool IsMerging { get; }
		public bool HasFirstChild { get; }
		public bool HasFirstParent => parentIds.Count > 0;
		public bool HasSecondParent => parentIds.Count > 1;
		public Commit FirstParent => Repository.Commits[parentIds[0]];
		public Commit SecondParent => Repository.Commits[parentIds[1]];
		public IEnumerable<Commit> Children => childIds.Select(id => Repository.Commits[id]);
		public Branch Branch => Repository.Branches[branchId];
		public bool IsMergePoint => parentIds.Count > 1;
		public bool IsCurrent => Id == Repository.CurrentCommit.Id
			&& Repository.CurrentBranch == Branch;
		public string WorkingFolder => Repository.MRepository.WorkingFolder;
		public Repository Repository { get; }

		public Task<IEnumerable<CommitFile>> FilesTask => Repository.CommitsFiles.GetAsync(CommitSha);

		public override string ToString() => $"{Id} {Subject} {CommitDate}";

		protected override bool IsEqual(Commit other) => Id == other.Id;

		protected override int GetHash() => Id.GetHashCode();
	}
}