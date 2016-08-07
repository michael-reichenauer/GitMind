using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.GitModel.Private;


namespace GitMind.GitModel
{
	internal class Commit
	{
		public static readonly string UncommittedId = MCommit.UncommittedId;

		private readonly Repository repository;
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
			string specifiedBranchName,
			bool isLocalAhead, 
			bool isRemoteAhead,
			bool isUncommitted,
			bool isVirtual)
		{
			this.repository = repository;
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
			IsLocalAhead = isLocalAhead;
			IsRemoteAhead = isRemoteAhead;
			IsUncommitted = isUncommitted;
			IsVirtual = isVirtual;
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
		public string SpecifiedBranchName { get; }
		public bool IsLocalAhead { get; }
		public bool IsRemoteAhead { get; }
		public bool IsUncommitted { get; }
		public bool IsVirtual { get; }
		public bool HasFirstParent => parentIds.Count > 0;
		public bool HasSecondParent => parentIds.Count > 1;
		public Commit FirstParent => repository.Commits[parentIds[0]];
		public Commit SecondParent => repository.Commits[parentIds[1]];
		public IEnumerable<Commit> Children => childIds.Select(id => repository.Commits[id]);
		public Branch Branch => repository.Branches[branchId];
		public bool IsMergePoint => parentIds.Count > 1;
		public bool IsCurrent => CommitId == repository.CurrentCommit.Id
			&& repository.CurrentBranch == Branch;
		public string WorkingFolder => repository.MRepository.WorkingFolder;

		//public IEnumerable<CommitFile> Files => repository.CommitsFiles[Id];
		public Task<IEnumerable<CommitFile>> FilesTask => repository.CommitsFiles.GetAsync(WorkingFolder, CommitId);



		public override string ToString() => $"{ShortId} {Subject} {CommitDate}";
	}
}