using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Git;
using GitMind.GitModel.Private;


namespace GitMind.GitModel
{
	internal class Commit
	{
		public static readonly string UncommittedId = MCommit.UncommittedId;

		private readonly IReadOnlyList<string> parentIds;
		private readonly IReadOnlyList<string> childIds;
		private readonly string branchId;

		public Commit(
			Repository repository, 
			string id, 
			string commitId, 
			string shortId, 
			string subject,
			string author, 
			DateTime authorDate, 
			DateTime commitDate,
			string tags, 
			string tickets, 
			string branchTips, 
			IReadOnlyList<string> parentIds, 
			IReadOnlyList<string> childIds, 
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
			ShortId = shortId;
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


		public string Id { get; }
		public string CommitId { get; }
		public string ShortId { get; }
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

		//public IEnumerable<CommitFile> Files => repository.CommitsFiles[Id];
		public Task<IEnumerable<CommitFile>> FilesTask => Repository.CommitsFiles.GetAsync(WorkingFolder, CommitId);
		


		public override string ToString() => $"{ShortId} {Subject} {CommitDate}";
	}
}