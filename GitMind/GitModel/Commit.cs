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
			string authorDate,
			string commitDate,
			IReadOnlyList<string> parentIds,
			IReadOnlyList<string> childIds,
			string branchId)
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
			CommitDate = commitDate;
		}


		public string Id { get; }
		public string ShortId { get; }
		public string Subject { get; }
		public string Author { get; }
		public string AuthorDate { get; }
		public string CommitDate { get; }
		public bool HasFirstParent => parentIds.Count > 0;
		public bool HasSecondParent => parentIds.Count > 1;
		public Commit FirstParent => repository.Commits[parentIds[0]];
		public Commit SecondParent => repository.Commits[parentIds[0]];
		public IEnumerable<Commit> Children => childIds.Select(id => repository.Commits[id]);
		public Branch Branch => repository.Branches[branchId];
	}
}