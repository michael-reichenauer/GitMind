using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.Git;


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
			BranchName name,
			string tipCommitId,
			string firstCommitId,
			string parentCommitId,
			IReadOnlyList<string> commitIds,
			string parentBranchId,
			IReadOnlyList<BranchName> childBranchNames,
			string mainBranchId,
			string localSubBranchId,
			bool isActive,
			bool isLocal,
			bool isRemote,
			bool isMainPart,
			bool isLocalPart,
			bool isMultiBranch,
			bool isDetached,
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
			MainBranchId = mainBranchId;
			LocalSubBranchId = localSubBranchId;
			IsActive = isActive;
			IsLocal = isLocal;
			IsRemote = isRemote;
			IsMainPart = isMainPart;
			IsLocalPart = isLocalPart;
			IsMultiBranch = isMultiBranch;
			LocalAheadCount = localAheadCount;
			RemoteAheadCount = remoteAheadCount;
			IsDetached = isDetached;
		}


		public string Id { get; }
		public BranchName Name { get; }
		public IReadOnlyList<BranchName> ChildBranchNames { get; }
		public string MainBranchId { get; }
		public string LocalSubBranchId { get; }
		public bool IsActive { get; }
		public bool IsLocal { get; }
		public bool IsRemote { get; }
		public bool IsMainPart { get; }
		public bool IsLocalPart { get; }
		public bool IsMultiBranch { get; }
		public int LocalAheadCount { get; }
		public int RemoteAheadCount { get; }
		public Commit TipCommit => Repository.Commits[tipCommitId];
		public Commit FirstCommit => Repository.Commits[firstCommitId];
		public Commit ParentCommit => Repository.Commits[parentCommitId];
		public IEnumerable<Commit> Commits => commitIds.Select(id => Repository.Commits[id]);
		public bool HasParentBranch => parentBranchId != null;
		public Branch ParentBranch => Repository.Branches[parentBranchId];
		public Branch LocalSubBranch => Repository.Branches[LocalSubBranchId];
		public Branch MainbBranch => Repository.Branches[MainBranchId];
		public bool IsCurrentBranch => Repository.CurrentBranch == this 
			|| IsMainPart && LocalSubBranch.IsCurrentBranch;
		public bool IsUncommited => IsCurrentBranch && !Repository.Status.isOk;
		public bool IsCanBeMergeToOther => !IsCurrentBranch && Repository.Status.isOk;
			public bool ICanBeMergeIntoThis => IsCurrentBranch && Repository.Status.isOk;
		public bool IsDetached { get; }
		public Repository Repository { get; }

		public bool CanBePublish => IsActive && IsLocal && !IsRemote && !IsLocalPart;

		public bool CanBePushed =>
			IsActive
			&& ((IsLocal && IsRemote) || (IsLocalPart && MainbBranch.RemoteAheadCount == 0))
			&& LocalAheadCount > 0
			&& RemoteAheadCount == 0;

		public bool CanBeUpdated =>
			IsActive
			&& ((IsCurrentBranch && !IsUncommited && RemoteAheadCount > 0)
			    || (IsCurrentBranch && !IsUncommited && IsLocalPart && MainbBranch.RemoteAheadCount > 0)
					|| (!IsCurrentBranch && RemoteAheadCount > 0 && !IsMainPart)
			    || (!IsCurrentBranch && IsLocalPart && MainbBranch.RemoteAheadCount > 0));



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


		public override string ToString() => IsLocalPart ? $"{Name} (local)" : $"{Name}";
	}
}