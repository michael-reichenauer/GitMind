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


		public async Task<MRepository> UpdateAsync(MRepository mRepository)
		{
			Status status = await statusService.GetStatusAsync();
			return await Task.Run(() => UpdateRepository(mRepository, status));
		}


		private MRepository UpdateRepository(MRepository repository, Status status)
		{
			string workingFolder = repository.WorkingFolder;

			try
			{
				Update(repository, status);
			}
			catch (Exception e)
			{
				Log.Error($"Failed to update repository {e}");

				Log.Debug("Retry from scratch using a new repository ...");

				repository = new MRepository()
				{
					WorkingFolder = workingFolder
				};

				Update(repository, status);
			}

			return repository;
		}


		private void Update(MRepository repository, Status status)
		{
			Log.Debug("Updating repository");
			Timing t = new Timing();
			string gitRepositoryPath = repository.WorkingFolder;

			using (GitRepository gitRepository = GitRepository.Open(diffService, gitRepositoryPath))
			{
				repository.Status = status;
				t.Log("Got git status");

				CleanRepositoryOfTempData(repository);

				commitsService.AddBranchCommits(gitRepository, status, repository);
				t.Log($"Added {repository.Commits.Count} commits referenced by active branches");

				AnalyzeBranchStructure(repository, status, gitRepository);
				t.Log("AnalyzeBranchStructure");
			}

			t.Log("Done");
		}



		private void AnalyzeBranchStructure(
			MRepository repository,
			Status status,
			GitRepository gitRepository)
		{
			branchService.AddActiveBranches(gitRepository, status, repository);

			MSubBranch mSubBranch = repository.SubBranches
				.FirstOrDefault(b => b.Value.Name == BranchName.Master && !b.Value.IsRemote).Value;
			MCommit commit = mSubBranch.TipCommit.FirstAncestors().Last();

			IReadOnlyList<CommitBranchName> gitSpecifiedNames = gitCommitsService.GetSpecifiedNames(
				commit.Id);

			IReadOnlyList<CommitBranchName> commitBranches = gitCommitsService.GetCommitBranches(
				commit.Id);

			commitBranchNameService.SetSpecifiedCommitBranchNames(gitSpecifiedNames, repository);
			commitBranchNameService.SetCommitBranchNames(commitBranches, repository);

			commitBranchNameService.SetMasterBranchCommits(repository);

			branchService.AddInactiveBranches(repository);

			commitBranchNameService.SetBranchTipCommitsNames(repository);

			commitBranchNameService.SetNeighborCommitNames(repository);

			branchService.AddMissingInactiveBranches(repository);

			branchService.AddMultiBranches(repository);

			branchHierarchyService.SetBranchHierarchy(repository);

			//aheadBehindService.SetAheadBehind(repository);

			tagService.AddTags(gitRepository, repository);

			MBranch currentBranch = repository.Branches.Values.First(b => b.IsActive && b.IsCurrent);
			repository.CurrentBranchId = currentBranch.Id;

			repository.CurrentCommitId = status.IsOK
				? gitRepository.Head.TipId
				: MCommit.UncommittedId;

			if (currentBranch.TipCommit.IsVirtual
					&& currentBranch.TipCommit.FirstParentId == repository.CurrentCommitId)
			{
				repository.CurrentCommitId = currentBranch.TipCommit.Id;
			}

			repository.SubBranches.Clear();
		}


		private static void CleanRepositoryOfTempData(MRepository repository)
		{
			RemoveVirtualCommits(repository);

			repository.Branches.Values.ForEach(b => b.TipCommit.BranchTips = null);

			repository.Commits.Values.ForEach(c => c.BranchTipBranches.Clear());
		}



		private static void RemoveVirtualCommits(MRepository repository)
		{
			List<MCommit> virtualCommits = repository.Commits.Values.Where(c => c.IsVirtual).ToList();
			foreach (MCommit virtualCommit in virtualCommits)
			{
				repository.ChildIds(virtualCommit.FirstParentId).Remove(virtualCommit.Id);
				repository.FirstChildIds(virtualCommit.FirstParentId).Remove(virtualCommit.Id);
				repository.Commits.Remove(virtualCommit.Id);
				virtualCommit.Branch.CommitIds.Remove(virtualCommit.Id);
				if (virtualCommit.Branch.TipCommitId == virtualCommit.Id)
				{
					virtualCommit.Branch.TipCommitId = virtualCommit.FirstParentId;
				}
			}
		}
	}
}