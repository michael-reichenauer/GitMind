using System.Collections.Generic;
using System.Linq;


namespace GitMind.GitModel
{
	// Some extra Branch
	internal class Branch
	{
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
			bool isLocal,
			bool isRemote,
			bool isMultiBranch,
			int localAheadCount,
			int remoteAheadCount)
		{
			this.Repository = repository;
			this.tipCommitId = tipCommitId;
			this.firstCommitId = firstCommitId;
			this.parentCommitId = parentCommitId;
			this.commitIds = commitIds;
			this.parentBranchId = parentBranchId;
			Id = id;
			Name = name;
			ChildBranchNames = childBranchNames;
			IsActive = isActive;
			IsLocal = isLocal;
			IsRemote = isRemote;
			IsMultiBranch = isMultiBranch;
			LocalAheadCount = localAheadCount;
			RemoteAheadCount = remoteAheadCount;
		}


		public string Id { get; }
		public string Name { get; }
		public IReadOnlyList<string> ChildBranchNames { get; }
		public bool IsActive { get; }
		public bool IsLocal { get; }
		public bool IsRemote { get; }
		public bool IsMultiBranch { get; }
		public int LocalAheadCount { get; }
		public int RemoteAheadCount { get; }
		public Commit TipCommit => Repository.Commits[tipCommitId];
		public Commit FirstCommit => Repository.Commits[firstCommitId];
		public Commit ParentCommit => Repository.Commits[parentCommitId];
		public IEnumerable<Commit> Commits => commitIds.Select(id => Repository.Commits[id]);
		public bool HasParentBranch => parentBranchId != null;
		public Branch ParentBranch => Repository.Branches[parentBranchId];
		public bool IsCurrentBranch => Repository.CurrentBranch == this;
		public bool IsMergeable => !IsCurrentBranch;
		public Repository Repository { get; }


		public IEnumerable<Branch> GetChildBranches()
		{
			foreach (Branch branch in Repository.Branches
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