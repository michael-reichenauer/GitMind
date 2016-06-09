using System;
using System.Collections.Generic;
using System.Linq;


namespace GitMind.GitModel
{
	internal class Commit
	{
		private readonly Repository repository;
		private readonly IReadOnlyList<string> parentIds;
		private readonly IReadOnlyList<string> childIds;
		private readonly string branchId;

		public Commit(
			Repository repository, 
			string id, 
			string shortId, 
			string subject, 
			string author, 
			DateTime authorDate, 
			DateTime commitDate, 
			IReadOnlyList<string> parentIds, 
			IReadOnlyList<string> childIds, 
			string branchId, 
			bool isLocalAhead, 
			bool isRemoteAhead)
		{
			this.repository = repository;
			this.parentIds = parentIds;
			this.childIds = childIds;
			this.branchId = branchId;
			Id = id;
			ShortId = shortId;
			Subject = subject;
			Author = author;
			AuthorDate = authorDate;			
			AuthorDateText = authorDate.ToShortDateString() + " " + authorDate.ToShortTimeString();
			CommitDate = commitDate;
			IsLocalAhead = isLocalAhead;
			IsRemoteAhead = isRemoteAhead;
		}


		public string Id { get; }
		public string ShortId { get; }
		public string Subject { get; }
		public string Author { get; }
		public DateTime AuthorDate { get; }
		public string AuthorDateText { get; }
		public DateTime CommitDate { get; }
		public bool IsLocalAhead { get; }
		public bool IsRemoteAhead { get; }
		public bool HasFirstParent => parentIds.Count > 0;
		public bool HasSecondParent => parentIds.Count > 1;
		public Commit FirstParent => repository.Commits[parentIds[0]];
		public Commit SecondParent => repository.Commits[parentIds[1]];
		public IEnumerable<Commit> Children => childIds.Select(id => repository.Commits[id]);
		public Branch Branch => repository.Branches[branchId];
		public bool IsMergePoint => parentIds.Count > 1;

		public override string ToString() => $"{ShortId} {Subject} {CommitDate}";
	}
}