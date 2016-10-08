using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class RepositoryService : IRepositoryService
	{
		private readonly IGitCommitsService gitCommitsService;
		private readonly ICacheService cacheService;
		private readonly ICommitsService commitsService;
		private readonly IBranchService branchService;
		private readonly ICommitBranchNameService commitBranchNameService;
		private readonly IBranchHierarchyService branchHierarchyService;
		//private readonly IAheadBehindService aheadBehindService;
		private readonly ITagService tagService;


		public RepositoryService()
			: this(
					new GitCommitsService(),
					new CacheService(),
					new CommitsService(),
					new BranchService(),
					new CommitBranchNameService(),
					new BranchHierarchyService(),
					//new AheadBehindService(),
					new TagService())
		{
		}


		public RepositoryService(
			IGitCommitsService gitCommitsService,
			ICacheService cacheService,
			ICommitsService commitsService,
			IBranchService branchService,
			ICommitBranchNameService commitBranchNameService,
			IBranchHierarchyService branchHierarchyService,
			//IAheadBehindService aheadBehindService,
			ITagService tagService)
		{
			this.gitCommitsService = gitCommitsService;
			this.cacheService = cacheService;
			this.commitsService = commitsService;
			this.branchService = branchService;
			this.commitBranchNameService = commitBranchNameService;
			this.branchHierarchyService = branchHierarchyService;
			//this.aheadBehindService = aheadBehindService;
			this.tagService = tagService;
		}


		public bool IsRepositoryCached(string workingFolder)
		{
			return cacheService.IsRepositoryCached(workingFolder);
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

				await UpdateAsync(mRepository);
				t.Log("Updated mRepository");
				cacheService.CacheAsync(mRepository).RunInBackground();
			}

			Repository repository = ToRepository(mRepository);
			t.Log($"Repository {repository.Branches.Count} branches, {repository.Commits.Count} commits");

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
			Log.Debug("Updated to repository");

			return repository;
		}


		public Task SetSpecifiedCommitBranchAsync(
			string gitRepositoryPath, 
			string commitId, 
			string rootId,
			BranchName branchName, 
			ICredentialHandler credentialHandler)
		{
			return gitCommitsService.EditCommitBranchAsync(
				gitRepositoryPath, commitId, rootId, branchName, credentialHandler);
		}


		private Task<MRepository> UpdateAsync(MRepository mRepository)
		{
			return Task.Run(() => UpdateRepository(mRepository));
		}


		private MRepository UpdateRepository(MRepository repository)
		{
			string workingFolder = repository.WorkingFolder;

			try
			{
				Update(repository);
			}
			catch (Exception e)
			{
				Log.Error($"Failed to update repository {e}");
				
				Log.Debug("Retry from scratch using a new repository ...");
				
				repository = new MRepository();
				repository.WorkingFolder = workingFolder;
				Update(repository);
			}

			return repository;
		}


		private void Update(MRepository repository)
		{
			Log.Debug("Updating repository");
			Timing t = new Timing();
			string gitRepositoryPath = repository.WorkingFolder;

			using (GitRepository gitRepository = GitRepository.Open(gitRepositoryPath))
			{
				GitStatus gitStatus = gitRepository.Status;
				repository.Status = gitStatus;
				t.Log("Got git status");

				CleanRepositoryOfTempData(repository);

				commitsService.AddBranchCommits(gitRepository, gitStatus, repository);
				t.Log($"Added {repository.Commits.Count} commits referenced by active branches");

				AnalyzeBranchStructure(repository, gitStatus, gitRepository);
				t.Log("AnalyzeBranchStructure");
			}

			t.Log("Done");
		}


		private void AnalyzeBranchStructure(
			MRepository repository, 
			GitStatus gitStatus, 
			GitRepository gitRepository)
		{
			string gitRepositoryPath = repository.WorkingFolder;		

			branchService.AddActiveBranches(gitRepository, gitStatus, repository);

			MSubBranch mSubBranch = repository.SubBranches
				.FirstOrDefault(b => b.Value.Name == BranchName.Master && !b.Value.IsRemote).Value;
			MCommit commit = mSubBranch.TipCommit.FirstAncestors().Last();

			IReadOnlyList<CommitBranchName> gitSpecifiedNames = gitCommitsService.GetSpecifiedNames(
				gitRepositoryPath, commit.Id);

			IReadOnlyList<CommitBranchName> commitBranches = gitCommitsService.GetCommitBranches(
				gitRepositoryPath, commit.Id);

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

			repository.CurrentCommitId = gitStatus.OK
				? gitRepository.Head.TipId
				: MCommit.UncommittedId;

			repository.SubBranches.Clear();
		}


		private static Repository ToRepository(MRepository mRepository)
		{
			KeyedList<string, Branch> rBranches = new KeyedList<string, Branch>(b => b.Id);
			KeyedList<string, Commit> rCommits = new KeyedList<string, Commit>(c => c.Id);
			Branch currentBranch = null;
			Commit currentCommit = null;
			MCommit rootCommit = mRepository.Branches
				.First(b => b.Value.Name == BranchName.Master && b.Value.IsActive)
				.Value.FirstCommit;

			Repository repository = new Repository(
				mRepository,
				new Lazy<IReadOnlyKeyedList<string, Branch>>(() => rBranches),
				new Lazy<IReadOnlyKeyedList<string, Commit>>(() => rCommits),
				new Lazy<Branch>(() => currentBranch),
				new Lazy<Commit>(() => currentCommit),
				mRepository.CommitsFiles,
				ToStatus(mRepository),
				rootCommit.Id);

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


		private static Status ToStatus(MRepository mRepository)
		{
			int statusCount = mRepository.Status?.Count ?? 0;

			int conflictCount = mRepository.Status?.ConflictCount ?? 0;
			string message = mRepository.Status?.Message;
			bool isMerging = mRepository.Status?.IsMerging ?? false;

			return new Status(statusCount, conflictCount, message, isMerging);
		}


		private static void CleanRepositoryOfTempData(MRepository repository)
		{
			RemoveVirtualCommits(repository);

			repository.Branches.Values.ForEach(b => b.TipCommit.BranchTips = null);

			repository.Commits.Values.ForEach(c => c.BranchTipBranches.Clear());
		}

		private static void RemoveVirtualCommits(MRepository repository)
		{
			//MCommit uncommitted;
			//if (repository.Commits.TryGetValue(MCommit.UncommittedId, out uncommitted))
			//{
			//	repository.ChildIds(uncommitted.FirstParentId).Remove(uncommitted.Id);
			//	repository.FirstChildIds(uncommitted.FirstParentId).Remove(uncommitted.Id);
			//	repository.Commits.Remove(uncommitted.Id);
			//	uncommitted.Branch.CommitIds.Remove(uncommitted.Id);
			//	if (uncommitted.Branch.TipCommitId == uncommitted.Id)
			//	{
			//		uncommitted.Branch.TipCommitId = uncommitted.FirstParentId;
			//	}
			//}


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