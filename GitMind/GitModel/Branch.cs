using System.Collections.Generic;
using System.Linq;


namespace GitMind.GitModel
{
	internal class Branch
	{
		private readonly Repository repository;
		private readonly string latestCommitId;
		private readonly string firstCommitId;
		private readonly string parentCommitId;
		private readonly IReadOnlyList<string> commitIds;
		private readonly string parentBranchId;

		public Branch(
			Repository repository,
			string id,
			string name,
			string latestCommitId,
			string firstCommitId,
			string parentCommitId,
			IReadOnlyList<string> commitIds,
			string parentBranchId,
			bool isActive,
			bool isMultiBranch)
		{
			this.repository = repository;
			this.latestCommitId = latestCommitId;
			this.firstCommitId = firstCommitId;
			this.parentCommitId = parentCommitId;
			this.commitIds = commitIds;
			this.parentBranchId = parentBranchId;
			Id = id;
			Name = name;
			IsActive = isActive;
			IsMultiBranch = isMultiBranch;
		}


		public string Id { get; }
		public string Name { get; }
		public bool IsActive { get; }
		public bool IsMultiBranch { get; }
		public Commit LatestCommit => repository.Commits[latestCommitId];
		public Commit FirstCommit => repository.Commits[firstCommitId];
		public Commit ParentCommit => repository.Commits[parentCommitId];
		public IEnumerable<Commit> Commits => commitIds.Select(id => repository.Commits[id]);
		public bool HasParentBranch => parentBranchId != null;
		public Branch ParentBranch => repository.Branches[parentBranchId];



		public IEnumerable<Branch> Parents()
		{
			Branch current = this;
			while (current.HasParentBranch)
			{
				current = current.ParentBranch;
				yield return current;
			}
		}

		public override string ToString() => Name;
	}
}