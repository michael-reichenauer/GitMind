using System.Collections.Generic;
using System.Linq;


namespace GitMind.GitModel
{
	// Some extra Branch
	internal class Branch
	{
		private readonly Repository repository;
		private readonly string tipCommitId;
		private readonly string firstCommitId;
		private readonly string parentCommitId;
		private readonly IReadOnlyList<string> commitIds;
		private readonly string parentBranchId;

		public Branch(
			Repository repository,
			string id,
			string name,
			string tipCommitId,
			string firstCommitId,
			string parentCommitId,
			IReadOnlyList<string> commitIds,
			string parentBranchId,
			IReadOnlyList<string> childBranchNames,
			bool isActive,
			bool isMultiBranch,
			int localAheadCount,
			int remoteAheadCount)
		{
			this.repository = repository;
			this.tipCommitId = tipCommitId;
			this.firstCommitId = firstCommitId;
			this.parentCommitId = parentCommitId;
			this.commitIds = commitIds;
			this.parentBranchId = parentBranchId;
			Id = id;
			Name = name;
			ChildBranchNames = childBranchNames;
			IsActive = isActive;
			IsMultiBranch = isMultiBranch;
			LocalAheadCount = localAheadCount;
			RemoteAheadCount = remoteAheadCount;
		}


		public string Id { get; }
		public string Name { get; }
		public IReadOnlyList<string> ChildBranchNames { get; }
		public bool IsActive { get; }
		public bool IsMultiBranch { get; }
		public int LocalAheadCount { get; }
		public int RemoteAheadCount { get; }
		public Commit TipCommit => repository.Commits[tipCommitId];
		public Commit FirstCommit => repository.Commits[firstCommitId];
		public Commit ParentCommit => repository.Commits[parentCommitId];
		public IEnumerable<Commit> Commits => commitIds.Select(id => repository.Commits[id]);
		public bool HasParentBranch => parentBranchId != null;
		public Branch ParentBranch => repository.Branches[parentBranchId];
		public bool IsCurrentBranch => repository.CurrentBranch == this;
		public bool IsMergeable =>
			!IsCurrentBranch
			&& repository.Status.ConflictCount == 0
			&& repository.Status.StatusCount == 0;


		public IEnumerable<Branch> GetChildBranches()
		{
			foreach (Branch branch in repository.Branches
				.Where(b => b.HasParentBranch && b.ParentBranch == this)
				.Distinct()
				.OrderByDescending(b => b.ParentCommit.CommitDate))
			{
				yield return branch;
			}
		}

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