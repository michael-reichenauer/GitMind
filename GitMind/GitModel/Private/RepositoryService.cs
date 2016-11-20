using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Features.Diffing;
using GitMind.Features.StatusHandling;
using GitMind.Features.StatusHandling.Private;
using GitMind.Git;
using GitMind.RepositoryViews;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	[SingleInstance]
	internal class RepositoryService : IRepositoryService, IRepositoryMgr
	{
		private readonly IStatusService statusService;
		private readonly IGitCommitsService gitCommitsService;
		private readonly ICacheService cacheService;
		private readonly ICommitsService commitsService;
		private readonly IBranchService branchService;
		private readonly ICommitBranchNameService commitBranchNameService;
		private readonly IBranchHierarchyService branchHierarchyService;
		private readonly ITagService tagService;
		private readonly ICommitsFiles commitsFiles;
		private readonly IDiffService diffService;


		public RepositoryService(
			IStatusService statusService,
			IGitCommitsService gitCommitsService,
			ICacheService cacheService,
			ICommitsService commitsService,
			IBranchService branchService,
			ICommitBranchNameService commitBranchNameService,
			IBranchHierarchyService branchHierarchyService,
			ITagService tagService,
			ICommitsFiles commitsFiles,
			IDiffService diffService)
		{
			this.statusService = statusService;
			this.gitCommitsService = gitCommitsService;
			this.cacheService = cacheService;
			this.commitsService = commitsService;
			this.branchService = branchService;
			this.commitBranchNameService = commitBranchNameService;
			this.branchHierarchyService = branchHierarchyService;
			this.tagService = tagService;
			this.commitsFiles = commitsFiles;
			this.diffService = diffService;
		}

		public event EventHandler<StatusChangedEventArgs> StatusChanged
		{
			add { statusService.StatusChanged += value; }
			remove { statusService.StatusChanged -= value; }
		}

		public event EventHandler<RepoChangedEventArgs> RepoChanged
		{
			add { statusService.RepoChanged += value; }
			remove { statusService.RepoChanged -= value; }
		}

		public Repository Repository { get; private set; }


		public void Monitor(string workingFolder)
		{
			statusService.Monitor(workingFolder);
		}


		public bool IsRepositoryCached(string workingFolder)
		{
			return cacheService.IsRepositoryCached(workingFolder);
		}


		public async Task InitialCachedOrFreshRepositoryAsync(string workingFolder)
		{
			Repository = await GetRepositoryAsync(true, workingFolder);
		}


		public async Task UpdateFreshRepositoryAsync()
		{
			Repository = await GetRepositoryAsync(false, Repository.MRepository.WorkingFolder);
		}


		public async Task UpdateRepositoryAsync()
		{
			Repository = await GetRepositoryAsync(false, Repository.MRepository.WorkingFolder);
		}


		private async Task<Repository> GetRepositoryAsync(bool useCache, string workingFolder)
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

			Timing t = new Timing();

			await UpdateAsync(mRepository);
			t.Log("Updated mRepository");
			cacheService.CacheAsync(mRepository).RunInBackground();

			Repository repository = ToRepository(mRepository);
			int branchesCount = repository.Branches.Count;
			int commitsCount = repository.Commits.Count;

			t.Log($"Updated repository {branchesCount} branches, {commitsCount} commits");
			Log.Debug("Updated to repository");

			return repository;
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

				repository = new MRepository()
				{
					WorkingFolder = workingFolder
				};

				Update(repository);
			}

			return repository;
		}


		private void Update(MRepository repository)
		{
			Log.Debug("Updating repository");
			Timing t = new Timing();
			string gitRepositoryPath = repository.WorkingFolder;

			// repository.Tips = GitRepository.GetRefsIds(gitRepositoryPath);
			using (GitRepository gitRepository = GitRepository.Open(diffService, gitRepositoryPath))
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
			branchService.AddActiveBranches(gitRepository, gitStatus, repository);

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

			repository.CurrentCommitId = gitStatus.OK
				? gitRepository.Head.TipId
				: MCommit.UncommittedId;

			if (currentBranch.TipCommit.IsVirtual
					&& currentBranch.TipCommit.FirstParentId == repository.CurrentCommitId)
			{
				repository.CurrentCommitId = currentBranch.TipCommit.Id;
			}

			repository.SubBranches.Clear();
		}


		private Repository ToRepository(MRepository mRepository)
		{
			Timing t = new Timing();
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
				commitsFiles,
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

			t.Log($"Created repositrory {repository.Commits.Count} commits");
			return repository;
		}


		private static Status ToStatus(MRepository mRepository)
		{
			Timing t = new Timing();
			int statusCount = mRepository.Status?.Count ?? 0;

			int conflictCount = mRepository.Status?.ConflictCount ?? 0;
			string message = mRepository.Status?.Message;
			bool isMerging = mRepository.Status?.IsMerging ?? false;

			Status status = new Status(statusCount, conflictCount, message, isMerging);
			t.Log("Got status");
			return status;
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