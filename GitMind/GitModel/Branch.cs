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
			string parentBranchId)
		{
			this.repository = repository;
			this.latestCommitId = latestCommitId;
			this.firstCommitId = firstCommitId;
			this.parentCommitId = parentCommitId;
			this.commitIds = commitIds;
			this.parentBranchId = parentBranchId;
			Id = id;
			Name = name;
		}


		public string Id { get; }
		public string Name { get; set; }
		public Commit LatestCommit => repository.Commits[latestCommitId];
		public Commit FirstCommit => repository.Commits[firstCommitId];
		public Commit ParentCommit => repository.Commits[parentCommitId];
		public IEnumerable<Commit> Commits => commitIds.Select(id => repository.Commits[id]);
		public bool HasParentBranch => parentBranchId != null;
		public Branch ParentBranch => repository.Branches[parentBranchId];
	}
}