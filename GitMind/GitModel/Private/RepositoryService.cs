using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Common;
using GitMind.Common.ProgressHandling;
using GitMind.Features.Remote;
using GitMind.Features.StatusHandling;
using GitMind.RepositoryViews;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind.GitModel.Private
{
	[SingleInstance]
	internal class RepositoryService : IRepositoryService, IRepositoryMgr
	{
		private static readonly TimeSpan RemoteRepositoryInterval = TimeSpan.FromSeconds(15);
		private static readonly TimeSpan MinCreateTimeBeforeCaching = TimeSpan.FromMilliseconds(1000);


		private readonly IStatusService statusService;
		private readonly ICacheService cacheService;
		private readonly ICommitsFiles commitsFiles;
		private readonly Lazy<IRemoteService> remoteService;
		private readonly IRepositoryStructureService repositoryStructureService;
		private readonly IProgressService progressService;
		private readonly IBranchTipMonitorService branchTipMonitorService;

		private DateTime fetchedTime = DateTime.MinValue;
		private AsyncLock syncRootAsync = new AsyncLock();

		public RepositoryService(
			IStatusService statusService,
			ICacheService cacheService,
			ICommitsFiles commitsFiles,
			Lazy<IRemoteService> remoteService,
			IRepositoryStructureService repositoryStructureService,
			IProgressService progressService,
			IBranchTipMonitorService branchTipMonitorService)
		{
			this.statusService = statusService;
			this.cacheService = cacheService;
			this.commitsFiles = commitsFiles;
			this.remoteService = remoteService;
			this.repositoryStructureService = repositoryStructureService;
			this.progressService = progressService;
			this.branchTipMonitorService = branchTipMonitorService;

			statusService.StatusChanged += (s, e) => OnStatusChanged(e.NewStatus);
			statusService.RepoChanged += (s, e) => OnRepoChanged(e.BranchIds);
		}

		public Repository Repository { get; private set; }

		public bool IsPaused => statusService.IsPaused;

		public event EventHandler<RepositoryUpdatedEventArgs> RepositoryUpdated;

		public event EventHandler<RepositoryErrorEventArgs> RepositoryErrorChanged;


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

			R<Repository> repository = await GetCachedRepositoryAsync(workingFolder);
			if (!repository.IsOk)
			{
				repository = await GetFreshRepositoryAsync(workingFolder, null);
			}

			Repository = repository.Value;
		}


		public async Task GetFreshRepositoryAsync()
		{
			string workingFolder = Repository.MRepository.WorkingFolder;
			R<Repository> repository = await GetFreshRepositoryAsync(workingFolder, Repository.MRepository.GitCommits);

			if (repository.IsOk)
			{
				Repository = repository.Value;
				RepositoryUpdated?.Invoke(this, new RepositoryUpdatedEventArgs());
			}
		}


		public Task CheckLocalRepositoryAsync()
		{
			return UpdateRepositoryAsync(null, null);
		}


		public async Task CheckBranchTipCommitsAsync()
		{
			Timing t = new Timing();
			await branchTipMonitorService.CheckAsync(Repository);
			t.Log("Check branch tips");
		}


		public async Task UpdateRepositoryAfterCommandAsync()
		{
			Task<GitStatus2> statusTask = statusService.GetStatusAsync();
			Task<IReadOnlyList<string>> repoIdsTask = statusService.GetRepoIdsAsync();

			GitStatus2 status = await statusTask;
			IReadOnlyList<string> repoIds = await repoIdsTask;

			if (Repository.Status.IsSame(status)
					&& Repository.MRepository.RepositoryIds.SequenceEqual(repoIds))
			{
				Log.Debug("Repository has not changed after command");
				return;
			}

			await UpdateRepositoryAsync(status, repoIds);
		}


		public async Task RefreshAfterCommandAsync(bool useFreshRepository)
		{
			Log.Debug("Refreshing after command ...");
			fetchedTime = DateTime.MinValue;
			await CheckRemoteChangesAsync(true);

			if (useFreshRepository)
			{
				Log.Debug("Getting fresh repository");
				await GetFreshRepositoryAsync();
			}
			else
			{
				await UpdateRepositoryAfterCommandAsync();
			}
		}


		public async Task CheckRemoteChangesAsync(bool isFetchNotes, bool isManual = false)
		{
			if (!isManual && Settings.Get<Options>().DisableAutoUpdate)
			{
				Log.Info("DisableAutoUpdate = true");
				return;
			}

			if (!isManual && DateTime.Now - fetchedTime < RemoteRepositoryInterval)
			{
				Log.Debug("No need the check remote yet");
				return;
			}

			Log.Debug("Fetching");

			if (isFetchNotes)
			{
				await remoteService.Value.FetchAllNotesAsync();
			}

			R result = await remoteService.Value.FetchAsync();
			RepositoryErrorChanged?.Invoke(this, new RepositoryErrorEventArgs(""));
			if (result.IsFaulted)
			{
				string text = $"Fetch error: {result.Error.Exception.Message}";
				Log.Warn(text);
				RepositoryErrorChanged?.Invoke(this, new RepositoryErrorEventArgs(text));
			}

			fetchedTime = DateTime.Now;
		}


		public async Task GetRemoteAndFreshRepositoryAsync(bool isManual)
		{
			Timing t = new Timing();
			fetchedTime = DateTime.MinValue;
			await CheckRemoteChangesAsync(true, isManual);
			t.Log("Remote check");
			await GetFreshRepositoryAsync();
			t.Log("Got Fresh Repository");
		}


		private async void OnRepoChanged(IReadOnlyList<string> repoIds)
		{
			if (Repository?.MRepository?.RepositoryIds.SequenceEqual(repoIds) ?? true)
			{
				Log.Debug("Same repo");
				return;
			}

			using (progressService.ShowBusy())
			{
				Log.Debug("Changed repo");
				await UpdateRepositoryAsync(null, repoIds);
			}
		}


		private async void OnStatusChanged(GitStatus2 status)
		{
			try
			{
				if (Repository?.Status?.IsSame(status) ?? true)
				{
					Log.Debug("Same status");
					return;
				}

				using (progressService.ShowBusy())
				{
					Log.Debug("Changed status");
					IReadOnlyList<string> repoIds = Repository.MRepository.RepositoryIds;
					await UpdateRepositoryAsync(status, repoIds);
				}
			}
			catch (Exception e)
			{
				Log.Exception(e, "Error handling status change");
			}
		}


		private async Task UpdateRepositoryAsync(GitStatus2 status, IReadOnlyList<string> repoIds)
		{
			if (Repository == null)
			{
				Log.Warn("No repository yet to update");
				return;
			}

			Repository = await UpdateRepositoryAsync(Repository, status, repoIds);

			RepositoryUpdated?.Invoke(this, new RepositoryUpdatedEventArgs());
		}


		private async Task<R<Repository>> GetFreshRepositoryAsync(
			string workingFolder, Dictionary<CommitId, GitCommit> gitCommits)
		{
			using (await syncRootAsync.LockAsync())
			{
				Log.Debug("No cached repository");
				MRepository mRepository = new MRepository();
				mRepository.WorkingFolder = workingFolder;

				mRepository.GitCommits = gitCommits ?? new Dictionary<CommitId, GitCommit>();

				Timing t = new Timing();
				await repositoryStructureService.UpdateAsync(mRepository, null, null);
				mRepository.TimeToCreateFresh = t.Elapsed;
				t.Log("Updated mRepository");

				if (mRepository.TimeToCreateFresh > MinCreateTimeBeforeCaching)
				{
					Log.Usage($"Caching repository ({t.Elapsed} ms)");
					await cacheService.CacheAsync(mRepository);
				}
				else
				{
					Log.Usage($"No need for cached repository ({t.Elapsed} ms)");
					cacheService.TryDeleteCache(workingFolder);
				}

				Repository repository = ToRepository(mRepository);
				t.Log($"Repository {repository.Branches.Count} branches, {repository.Commits.Count} commits");

				return repository;
			}
		}


		private async Task<Repository> UpdateRepositoryAsync(
			Repository sourcerepository, GitStatus2 status, IReadOnlyList<string> branchIds)
		{
			using (await syncRootAsync.LockAsync())
			{
				Log.Debug($"Updating repository");

				MRepository mRepository = sourcerepository.MRepository;
				mRepository.IsCached = false;

				Timing t = new Timing();

				await repositoryStructureService.UpdateAsync(mRepository, status, branchIds);
				t.Log("Updated mRepository");

				if (mRepository.TimeToCreateFresh > MinCreateTimeBeforeCaching)
				{
					Log.Usage($"Caching repository ({t.Elapsed} ms)");
					await cacheService.CacheAsync(mRepository);
				}

				Repository repository = ToRepository(mRepository);
				int branchesCount = repository.Branches.Count;
				int commitsCount = repository.Commits.Count;

				t.Log($"Updated repository {branchesCount} branches, {commitsCount} commits");
				Log.Debug("Updated to repository");

				return repository;
			}
		}


		private async Task<R<Repository>> GetCachedRepositoryAsync(string workingFolder)
		{
			try
			{
				Timing t = new Timing();
				MRepository mRepository = await cacheService.TryGetRepositoryAsync(workingFolder);

				if (mRepository != null)
				{
					t.Log("Read from cache");
					mRepository.IsCached = true;
					Repository repository = ToRepository(mRepository);
					int branchesCount = repository.Branches.Count;
					int commitsCount = repository.Commits.Count;
					t.Log($"Repository {branchesCount} branches, {commitsCount} commits");
					return repository;
				}

				return R<Repository>.NoValue;
			}
			catch (Exception e)
			{
				Log.Exception(e, "Failed to read cached repository");
				return e;
			}
			finally
			{
				cacheService.TryDeleteCache(workingFolder);
			}
		}


		private Repository ToRepository(MRepository mRepository)
		{
			Timing t = new Timing();
			KeyedList<string, Branch> rBranches = new KeyedList<string, Branch>(b => b.Id);
			Dictionary<CommitId, Commit> rCommits = new Dictionary<CommitId, Commit>();
			Branch currentBranch = null;
			Commit currentCommit = null;
			CommitId rootCommitId = mRepository.RootCommitId;

			Repository repository = new Repository(
				mRepository,
				new Lazy<IReadOnlyKeyedList<string, Branch>>(() => rBranches),
				new Lazy<IReadOnlyDictionary<CommitId, Commit>>(() => rCommits),
				new Lazy<Branch>(() => currentBranch),
				new Lazy<Commit>(() => currentCommit),
				commitsFiles,
				mRepository.Status,
				rootCommitId,
				mRepository.Uncommitted?.Id ?? CommitId.None);

			foreach (var mCommit in mRepository.Commits.Values)
			{
				Commit commit = Converter.ToCommit(repository, mCommit);
				rCommits[commit.Id] = commit;
				if (mCommit == mRepository.CurrentCommit)
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

			t.Log($"Created repository {repository.Commits.Count} commits");
			return repository;
		}
	}
}