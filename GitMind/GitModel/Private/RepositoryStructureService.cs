using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Features.Diffing;
using GitMind.Features.StatusHandling;
using GitMind.Git;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class RepositoryStructureService : IRepositoryStructureService
	{
		private readonly IBranchService branchService;
		private readonly IStatusService statusService;
		private readonly IGitCommitsService gitCommitsService;
		private readonly ICommitBranchNameService commitBranchNameService;
		private readonly IBranchHierarchyService branchHierarchyService;
		private readonly ITagService tagService;
		private readonly ICommitsService commitsService;
		private readonly IDiffService diffService;


		public RepositoryStructureService(
			IBranchService branchService,
			IStatusService statusService,
			IGitCommitsService gitCommitsService,
			ICommitBranchNameService commitBranchNameService,
			IBranchHierarchyService branchHierarchyService,
			ITagService tagService,
			ICommitsService commitsService,
			IDiffService diffService)
		{
			this.branchService = branchService;
			this.statusService = statusService;
			this.gitCommitsService = gitCommitsService;
			this.commitBranchNameService = commitBranchNameService;
			this.branchHierarchyService = branchHierarchyService;
			this.tagService = tagService;
			this.commitsService = commitsService;
			this.diffService = diffService;
		}


		public async Task<MRepository> UpdateAsync(
			MRepository mRepository, Status status, IReadOnlyList<string> repoIds)
		{		
			return await Task.Run(() => UpdateRepository(mRepository, status, repoIds));
		}


		private MRepository UpdateRepository(
			MRepository repository, Status status, IReadOnlyList<string> repoIds)
		{
			string workingFolder = repository.WorkingFolder;

			try
			{
				Update(repository, status, repoIds);
			}
			catch (Exception e)
			{
				Log.Error($"Failed to update repository {e}");

				Log.Debug("Retry from scratch using a new repository ...");

				repository = new MRepository()
				{
					WorkingFolder = workingFolder
				};

				Update(repository, status, repoIds);
			}

			return repository;
		}


		private void Update(MRepository repository, Status status, IReadOnlyList<string> repoIds)
		{
			Log.Debug("Updating repository");
			Timing t = new Timing();
			string gitRepositoryPath = repository.WorkingFolder;

			using (GitRepository gitRepository = GitRepository.Open(diffService, gitRepositoryPath))
			{
				repository.Status = status ?? statusService.GetStatus();
				t.Log("Got status");

				repository.RepositoryIds = repoIds ?? statusService.GetRepoIds();
				t.Log("Got repo ids");

				CleanRepositoryOfTempData(repository);
				t.Log("CleanRepositoryOfTempData");

				commitsService.AddBranchCommits(gitRepository, repository);
				t.Log($"Added {repository.Commits.Count} commits referenced by active branches");

				tagService.AddTags(gitRepository, repository);
				t.Log("AddTags");

				branchService.AddActiveBranches(gitRepository, repository);
				t.Log("AddActiveBranches");

				SetSpecifiedCommitBranchNames(repository);
				t.Log("SetSpecifiedCommitBranchNames");

				commitBranchNameService.SetMasterBranchCommits(repository);
				t.Log("SetMasterBranchCommits");

				branchService.AddInactiveBranches(repository);
				t.Log("AddInactiveBranches");

				commitBranchNameService.SetBranchTipCommitsNames(repository);
				t.Log("SetBranchTipCommitsNames");

				commitBranchNameService.SetNeighborCommitNames(repository);
				t.Log("SetNeighborCommitNames");

				branchService.AddMissingInactiveBranches(repository);
				t.Log("AddMissingInactiveBranches");

				branchService.AddMultiBranches(repository);
				t.Log("AddMultiBranches");

				branchHierarchyService.SetBranchHierarchy(repository);
				t.Log("SetBranchHierarchy");			

				SetCurrentBranchAndCommit(repository, gitRepository);
				t.Log("SetCurrentBranchAndCommit");

				repository.SubBranches.Clear();
				t.Log("Clear sub branches");
			}

			t.Log("Done");
		}


		private static void SetCurrentBranchAndCommit(
			MRepository repository, GitRepository gitRepository)
		{
			Status status = repository.Status;
			MBranch currentBranch = repository.Branches.Values.First(b => b.IsActive && b.IsCurrent);
			repository.CurrentBranchId = currentBranch.Id;

			repository.CurrentCommitId = status.IsOK
				? repository.Commit(gitRepository.Head.TipId).IndexId
				: repository.Uncommitted.IndexId;

			if (currentBranch.TipCommit.IsVirtual
			    && currentBranch.TipCommit.FirstParentId == repository.CurrentCommitId)
			{
				repository.CurrentCommitId = currentBranch.TipCommit.IndexId;
			}
		}


		private void SetSpecifiedCommitBranchNames(MRepository repository)
		{
			MCommit rootCommit = GetRootCommit(repository);

			IReadOnlyList<CommitBranchName> gitSpecifiedNames = gitCommitsService.GetSpecifiedNames(
				rootCommit.CommitId);

			IReadOnlyList<CommitBranchName> commitBranches = gitCommitsService.GetCommitBranches(
				rootCommit.CommitId);

			commitBranchNameService.SetSpecifiedCommitBranchNames(gitSpecifiedNames, repository);
			commitBranchNameService.SetCommitBranchNames(commitBranches, repository);
		}


		private static MCommit GetRootCommit(MRepository repository)
		{
			MSubBranch mSubBranch = repository.SubBranches
				.FirstOrDefault(b => b.Value.Name == BranchName.Master && !b.Value.IsRemote).Value;
			MCommit rootCommit = mSubBranch.TipCommit.FirstAncestors().Last();
			return rootCommit;
		}


		private static void CleanRepositoryOfTempData(MRepository repository)
		{
			RemoveVirtualCommits(repository);

			repository.Branches.Values.ForEach(b => b.TipCommit.BranchTips = null);

			repository.Commits.ForEach(c => c.BranchTipBranches.Clear());
		}



		private static void RemoveVirtualCommits(MRepository repository)
		{
			List<MCommit> virtualCommits = repository.Commits.Where(c => c.IsVirtual).ToList();
			foreach (MCommit virtualCommit in virtualCommits)
			{
				virtualCommit.FirstParent.ChildIds.Remove(virtualCommit.IndexId);
				virtualCommit.FirstParent.FirstChildIds.Remove(virtualCommit.IndexId);
				repository.Commits.Remove(virtualCommit);
				if (virtualCommit.CommitId != null)
				{
					repository.CommitsById.Remove(virtualCommit.CommitId);
				}
				virtualCommit.Branch.CommitIds.Remove(virtualCommit.IndexId);
				if (virtualCommit.Branch.TipCommitId == virtualCommit.IndexId)
				{
					virtualCommit.Branch.TipCommitId = virtualCommit.FirstParentId;
				}
			}

			repository.Uncommitted = null;
		}
	}
}