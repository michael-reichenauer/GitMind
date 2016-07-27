﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class RepositoryService : IRepositoryService
	{
		private readonly IGitService gitService;
		private readonly ICacheService cacheService;
		private readonly ICommitsService commitsService;
		private readonly IBranchService branchService;
		private readonly ICommitBranchNameService commitBranchNameService;
		private readonly IBranchHierarchyService branchHierarchyService;
		private readonly IAheadBehindService aheadBehindService;
		private readonly ITagService tagService;


		public RepositoryService()
			: this(
					new GitService(),
					new CacheService(),
					new CommitsService(),
					new BranchService(),
					new CommitBranchNameService(),
					new BranchHierarchyService(),
					new AheadBehindService(),
					new TagService())
		{
		}


		public RepositoryService(
			IGitService gitService,
			ICacheService cacheService,
			ICommitsService commitsService,
			IBranchService branchService,
			ICommitBranchNameService commitBranchNameService,
			IBranchHierarchyService branchHierarchyService,
			IAheadBehindService aheadBehindService,
			ITagService tagService)
		{
			this.gitService = gitService;
			this.cacheService = cacheService;
			this.commitsService = commitsService;
			this.branchService = branchService;
			this.commitBranchNameService = commitBranchNameService;
			this.branchHierarchyService = branchHierarchyService;
			this.aheadBehindService = aheadBehindService;
			this.tagService = tagService;
		}


		public Task<Repository> GetCachedOrFreshRepositoryAsync(string workingFolder)
		{
			return GetRepositoryAsync(true, workingFolder);
		}


		public Task<Repository> GetFreshRepositoryAsync(string workingFolder)
		{
			return GetRepositoryAsync(false, workingFolder);
		}


		public async Task<Repository> GetRepositoryAsync(bool useCache, string workingFolder)
		{
			Timing t = new Timing();
			MRepository mRepository = null;
			if (useCache)
			{
				mRepository = await cacheService.TryGetRepositoryAsync(workingFolder);
				t.Log("cacheService.TryGetRepositoryAsync");
			}

			if (mRepository == null)
			{
				Log.Debug("No cached repository");
				mRepository = new MRepository();
				mRepository.WorkingFolder = workingFolder;
				mRepository.CommitsFiles = new CommitsFiles();

				await UpdateAsync(mRepository);
				t.Log("Updated mRepository");
				cacheService.CacheAsync(mRepository).RunInBackground();
			}

			Repository repository = ToRepository(mRepository);
			t.Log($"Created repository: {repository.Branches.Count} commits: {repository.Commits.Count}");

			return repository;
		}


		public async Task<Repository> UpdateRepositoryAsync(Repository sourcerepository)
		{
			Log.Debug($"Updating repository");

			MRepository mRepository = sourcerepository.MRepository;
			mRepository.CommitsFiles = sourcerepository.CommitsFiles;

			Timing t = new Timing();

			await UpdateAsync(mRepository);
			t.Log("Updated mRepository");
			cacheService.CacheAsync(mRepository).RunInBackground();

			Repository repository = ToRepository(mRepository);
			t.Log($"Repository {repository.Branches.Count} branches, {repository.Commits.Count} commits");
			Log.Debug($"Updated to repository");

			return repository;
		}


		public Task SetSpecifiedCommitBranchAsync(
			string commitId, string branchName, string gitRepositoryPath)
		{
			return gitService.SetSpecifiedCommitBranchAsync(gitRepositoryPath, commitId, branchName);
		}


		private Task UpdateAsync(MRepository mRepository)
		{
			return Task.Run(() =>
			{
				Update(mRepository);
			});
		}


		private void Update(MRepository repository)
		{
			Log.Debug("Updating repository");
			Timing t = new Timing();
			string gitRepositoryPath = repository.WorkingFolder;

			IReadOnlyList<GitSpecifiedNames> specifiedNames = gitService.GetSpecifiedNames(
				gitRepositoryPath);

			using (GitRepository gitRepository = gitService.OpenRepository(gitRepositoryPath))
			{
				GitStatus gitStatus = gitRepository.Status;
				repository.Status = gitStatus;
				t.Log("Got git status");

				CleanRepositoryOfTempData(repository);

				commitsService.AddBranchCommits(gitRepository, gitStatus, repository);
				t.Log($"Added {repository.Commits.Count} commits referenced by active branches");

				AnalyzeBranchStructure(repository, specifiedNames, gitStatus, gitRepository);
				t.Log("AnalyzeBranchStructure");		
			}

			t.Log("Done");
		}




		private void AnalyzeBranchStructure(
			MRepository repository, 
			IReadOnlyList<GitSpecifiedNames> gitSpecifiedNames, 
			GitStatus gitStatus, 
			GitRepository gitRepository)
		{
			commitBranchNameService.SetSpecifiedCommitBranchNames(gitSpecifiedNames, repository);
		
			branchService.AddActiveBranches(gitRepository, gitStatus, repository);
			
			commitBranchNameService.SetMasterBranchCommits(repository);

			branchService.AddInactiveBranches(repository);

			commitBranchNameService.SetBranchTipCommitsNames(repository);

			commitBranchNameService.SetNeighborCommitNames(repository);

			branchService.AddMissingInactiveBranches(repository);

			branchService.AddMultiBranches(repository);

			branchHierarchyService.SetBranchHierarchy(repository);
			
			aheadBehindService.SetAheadBehind(repository);

			tagService.AddTags(gitRepository, repository);

			repository.CurrentBranchId = repository.Branches
				.First(b => b.Value.IsActive && b.Value.Name == gitRepository.Head.Name).Value.Id;

			repository.CurrentCommitId = gitStatus.OK
				? repository.Commits[gitRepository.Head.TipId].Id
				: MCommit.UncommittedId;

			repository.SubBranches.Clear();
		}


		private static Repository ToRepository(MRepository mRepository)
		{
			KeyedList<string, Branch> rBranches = new KeyedList<string, Branch>(b => b.Id);
			KeyedList<string, Commit> rCommits = new KeyedList<string, Commit>(c => c.Id);
			Branch currentBranch = null;
			Commit currentCommit = null;

			Repository repository = new Repository(
				mRepository,
				new Lazy<IReadOnlyKeyedList<string, Branch>>(() => rBranches),
				new Lazy<IReadOnlyKeyedList<string, Commit>>(() => rCommits),
				new Lazy<Branch>(() => currentBranch),
				new Lazy<Commit>(() => currentCommit),
				mRepository.CommitsFiles,
				new Status(mRepository.Status?.ConflictCount ?? 0));

			foreach (var mCommit in mRepository.Commits)
			{
				Commit commit = Converter.ToCommit(repository, mCommit.Value);
				rCommits.Add(commit);
				if (mCommit.Value == mRepository.CurrentCommit)
				{
					currentCommit = commit;
				}
			}

			

			foreach (var mBranch in mRepository.Branches)
			{
				Branch branch = Converter.ToBranch(repository, mBranch.Value);
				rBranches.Add(branch);

				if (mBranch.Value == mRepository.CurrentBranch)
				{
					currentBranch = branch;
				}
			}

			return repository;
		}


		private static void CleanRepositoryOfTempData(MRepository repository)
		{
			RemoveVirtualCommits(repository);

			repository.Branches.Values.ForEach(b => b.LatestCommit.BranchTips = null);

			repository.Commits.Values.ForEach(c => c.BranchTipBranches.Clear());
		}

		private static void RemoveVirtualCommits(MRepository repository)
		{
			MCommit uncommitted;
			if (repository.Commits.TryGetValue(MCommit.UncommittedId, out uncommitted))
			{
				repository.ChildIds(uncommitted.FirstParentId).Remove(uncommitted.Id);
				repository.FirstChildIds(uncommitted.FirstParentId).Remove(uncommitted.Id);
				repository.Commits.Remove(uncommitted.Id);
				uncommitted.Branch.CommitIds.Remove(uncommitted.Id);
				if (uncommitted.Branch.LatestCommitId == uncommitted.Id)
				{
					uncommitted.Branch.LatestCommitId = uncommitted.FirstParentId;
				}
			}


			//List<MCommit> virtualCommits = repository.Commits.Values.Where(c => c.IsVirtual).ToList();
			//foreach (MCommit virtualCommit in virtualCommits)
			//{
			//	repository.ChildIds(virtualCommit.FirstParentId).Remove(virtualCommit.Id);
			//	repository.FirstChildIds(virtualCommit.FirstParentId).Remove(virtualCommit.Id);
			//	repository.Commits.Remove(virtualCommit.Id);
			//	virtualCommit.Branch.CommitIds.Remove(virtualCommit.Id);
			//	if (virtualCommit.Branch.LatestCommitId == virtualCommit.Id)
			//	{
			//		virtualCommit.Branch.LatestCommitId = virtualCommit.FirstParentId;
			//	}
			//}
		}
	}
}