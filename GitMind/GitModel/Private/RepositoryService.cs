using System;
using System.Linq;
using System.Threading.Tasks;
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
		private readonly ICacheService cacheService;
		private readonly ICommitsFiles commitsFiles;
		private readonly IRepositoryStructureService repositoryStructureService;


		public RepositoryService(
			IStatusService statusService,
			ICacheService cacheService,
			ICommitsFiles commitsFiles,
			IRepositoryStructureService repositoryStructureService)
		{
			this.statusService = statusService;
			this.cacheService = cacheService;
			this.commitsFiles = commitsFiles;
			this.repositoryStructureService = repositoryStructureService;

			statusService.StatusChanged += (s, e) => OnStatusChanged(e.NewStatus);
			statusService.RepoChanged += (s, e) => OnRepoChanged();
		}

		public Repository Repository { get; private set; }

		public event EventHandler<RepositoryUpdatedEventArgs> RepositoryUpdated;


		public void Monitor(string workingFolder)
		{
			statusService.Monitor(workingFolder);
		}


		public bool IsRepositoryCached(string workingFolder)
		{
			return cacheService.IsRepositoryCached(workingFolder);
		}


		public async Task LoadRepositoryAsync(string workingFolder)
		{
			Monitor(workingFolder);

			Repository = await GetRepositoryAsync(true, workingFolder);
		}


		public async Task UpdateFreshRepositoryAsync()
		{
			Repository = await GetRepositoryAsync(false, Repository.MRepository.WorkingFolder);

			RepositoryUpdated?.Invoke(this, new RepositoryUpdatedEventArgs());
		}


		public Task UpdateRepositoryAsync()
		{
			return UpdateRepositoryAsync(null);
		}


		private async void OnRepoChanged()
		{
			await UpdateRepositoryAsync();
		}


		private async void OnStatusChanged(Status status)
		{
			if (Repository?.Status?.IsSame(status) ?? false)
			{
				Log.Debug("Same status");
				return;
			}

			await UpdateRepositoryAsync(status);
		}


		private async Task UpdateRepositoryAsync(Status status)
		{
			Repository = await UpdateRepositoryAsync(Repository, status);

			RepositoryUpdated?.Invoke(this, new RepositoryUpdatedEventArgs());
		}


		private async Task<Repository> GetRepositoryAsync(bool useCache, string workingFolder)
		{
			Timing t = new Timing();
			MRepository mRepository = null;
			Repository repository = null;
			bool usedCached = false;

			if (useCache)
			{
				try
				{
					mRepository = await cacheService.TryGetRepositoryAsync(workingFolder);
					usedCached = true;
					t.Log("cacheService.TryGetRepositoryAsync");
					if (mRepository != null)
					{
						repository = ToRepository(mRepository);
						t.Log($"Repository {repository.Branches.Count} branches, {repository.Commits.Count} commits");

						return repository;
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to read cached repositrory {e}");
					cacheService.TryDeleteCache(workingFolder);
				}
			}

			Log.Debug("No cached repository");
			mRepository = new MRepository();
			mRepository.WorkingFolder = workingFolder;

			await repositoryStructureService.UpdateAsync(mRepository, null);
			t.Log("Updated mRepository");
			if (!usedCached && t.Elapsed > TimeSpan.FromMilliseconds(1000))
			{
				Log.Usage($"Caching repository ({t.Elapsed} ms)");
				await cacheService.CacheAsync(mRepository);
			}
			else
			{
				Log.Usage($"No need for cached repository ({t.Elapsed} ms)");
				cacheService.TryDeleteCache(workingFolder);
			}
							
			repository = ToRepository(mRepository);
			t.Log($"Repository {repository.Branches.Count} branches, {repository.Commits.Count} commits");

			return repository;		
		}


		public async Task<Repository> UpdateRepositoryAsync(Repository sourcerepository, Status status)
		{
			Log.Debug($"Updating repository");

			MRepository mRepository = sourcerepository.MRepository;

			Timing t = new Timing();

			await repositoryStructureService.UpdateAsync(mRepository, status);
			t.Log("Updated mRepository");
			await cacheService.CacheAsync(mRepository);

			Repository repository = ToRepository(mRepository);
			int branchesCount = repository.Branches.Count;
			int commitsCount = repository.Commits.Count;

			t.Log($"Updated repository {branchesCount} branches, {commitsCount} commits");
			Log.Debug("Updated to repository");

			return repository;
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
				mRepository.Status,
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
	}
}