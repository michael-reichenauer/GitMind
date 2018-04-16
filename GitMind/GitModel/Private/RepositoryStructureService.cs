using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Features.StatusHandling;
using GitMind.Features.Tags;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;


namespace GitMind.GitModel.Private
{
	internal class RepositoryStructureService : IRepositoryStructureService
	{
		private readonly IBranchService branchService;
		private readonly IStatusService statusService;
		private readonly ICommitBranchNameService commitBranchNameService;
		private readonly IGitCommitBranchNameService gitCommitBranchNameService;
		private readonly IBranchHierarchyService branchHierarchyService;
		private readonly ITagService tagService;
		private readonly ICommitsService commitsService;
		private readonly IGitBranchService2 gitBranchService2;
		//private readonly IDiffService diffService;



		public RepositoryStructureService(
			IBranchService branchService,
			IStatusService statusService,
			ICommitBranchNameService commitBranchNameService,
			IGitCommitBranchNameService gitCommitBranchNameService,
			IBranchHierarchyService branchHierarchyService,
			ITagService tagService,
			ICommitsService commitsService,
			IGitBranchService2 gitBranchService2
			//IDiffService diffService
			)
		{
			this.branchService = branchService;
			this.statusService = statusService;
			this.commitBranchNameService = commitBranchNameService;
			this.gitCommitBranchNameService = gitCommitBranchNameService;
			this.branchHierarchyService = branchHierarchyService;
			this.tagService = tagService;
			this.commitsService = commitsService;
			this.gitBranchService2 = gitBranchService2;
			//this.diffService = diffService;
		}


		public async Task<MRepository> UpdateAsync(
			MRepository mRepository, GitStatus2 status, IReadOnlyList<string> repoIds)
		{
			return await Task.Run(async () =>
			{
				MRepository repository = mRepository;
				string workingFolder = repository.WorkingFolder;

				try
				{
					await UpdateRepositoryAsync(repository, status, repoIds);
				}
				catch (Exception e)
				{
					Log.Exception(e, "Failed to update repository");

					Log.Debug("Retry from scratch using a new repository ...");

					repository = new MRepository()
					{
						WorkingFolder = workingFolder
					};

					await UpdateRepositoryAsync(repository, status, repoIds);
				}

				return repository;
			});
		}


		private async Task UpdateRepositoryAsync(MRepository repository, GitStatus2 status, IReadOnlyList<string> repoIds)
		{
			Log.Debug("Updating repository");
			Timing t = new Timing();
			//string gitRepositoryPath = repository.WorkingFolder;

			var branchesResult = await gitBranchService2.GetBranchesAsync(CancellationToken.None);
			if (branchesResult.IsFaulted)
			{
				Log.Warn($"Failed to update repo, {branchesResult}");
				return;
			}

			IReadOnlyList<GitBranch2> branches = branchesResult.Value;
			//using (GitRepository gitRepository = GitRepository.Open(diffService, gitRepositoryPath))

			repository.Status = status ?? await statusService.GetStatusAsync();
			t.Log("Got status");

			repository.RepositoryIds = repoIds ?? await statusService.GetRepoIdsAsync();
			t.Log("Got repo ids");

			CleanRepositoryOfTempData(repository);
			t.Log("CleanRepositoryOfTempData");

			await commitsService.AddNewCommitsAsync(repository);
			t.Log("Added new commits");

			commitsService.AddBranchCommits(branches, repository);   
			t.Log($"Added {repository.Commits.Count} commits referenced by active branches");

			//if (!repository.Commits.Any())
			//{
			//	Log.Debug("No branches, no commits");
			//	return;
			//}

			await tagService.CopyTagsAsync(repository);
			t.Log("CopyTags");

			branchService.AddActiveBranches(branches, repository);
			t.Log("AddActiveBranches");

			await SetSpecifiedCommitBranchNamesAsync(repository);
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

			SetCurrentBranchAndCommit(repository, branches);
			t.Log("SetCurrentBranchAndCommit");

			repository.SubBranches.Clear();
			t.Log("Clear sub branches");


			t.Log("Done");
		}


		private static void SetCurrentBranchAndCommit(MRepository repository, IReadOnlyList<GitBranch2> branches)
		{
			GitStatus2 status = repository.Status;
			MBranch currentBranch = repository.Branches.Values.First(b => b.IsActive && b.IsCurrent);
			repository.CurrentBranchId = currentBranch.Id;
			branches.TryGetCurrent(out GitBranch2 current);
			repository.CurrentCommitId = status.OK
				? current != null
					? repository.Commit(new CommitId(current.TipSha.Sha)).Id
					: CommitId.NoCommits
				: CommitId.Uncommitted;

			if (currentBranch.TipCommit.IsVirtual
					&& currentBranch.TipCommit.FirstParentId == repository.CurrentCommitId)
			{
				repository.CurrentCommitId = currentBranch.TipCommit.Id;
			}
		}


		private async Task SetSpecifiedCommitBranchNamesAsync(MRepository repository)
		{
			MCommit rootCommit = GetRootCommit(repository);
			repository.RootCommitId = rootCommit.RealCommitId;

			IReadOnlyList<CommitBranchName> gitSpecifiedNames = await gitCommitBranchNameService.GetEditedBranchNamesAsync(
				rootCommit.Sha);

			IReadOnlyList<CommitBranchName> commitBranches = await gitCommitBranchNameService.GetCommitBranchNamesAsync(
				rootCommit.Sha);

			commitBranchNameService.SetSpecifiedCommitBranchNames(gitSpecifiedNames, repository);
			commitBranchNameService.SetCommitBranchNames(commitBranches, repository);
		}


		private static MCommit GetRootCommit(MRepository repository)
		{
			MSubBranch mSubBranch = repository.SubBranches
				.FirstOrDefault(b => b.Value.IsActive && b.Value.Name == BranchName.Master).Value;
			if (mSubBranch == null)
			{
				Asserter.FailFast($"Repository {repository.WorkingFolder} does not have a master branch");
			}

			IEnumerable<MCommit> firstAncestors = mSubBranch.TipCommit.FirstAncestorsAnSelf();
			MCommit rootCommit = firstAncestors.Last();
			return rootCommit;
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
				if (virtualCommit.ParentIds.Any())
				{
					virtualCommit.FirstParent.ChildIds.Remove(virtualCommit.Id);
					virtualCommit.FirstParent.FirstChildIds.Remove(virtualCommit.Id);

					virtualCommit.Branch.CommitIds.Remove(virtualCommit.Id);
					if (virtualCommit.Branch.TipCommitId == virtualCommit.Id)
					{
						virtualCommit.Branch.TipCommitId = virtualCommit.FirstParentId;
					}

					virtualCommit.ParentIds.Clear();

					repository.Commits.Remove(virtualCommit.Id);
					repository.GitCommits.Remove(virtualCommit.Id);
				}
			}

			repository.Uncommitted = null;
		}
	}
}