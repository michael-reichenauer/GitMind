using System;
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
		private readonly IAheadBehindService aheadBehindService;
		private readonly ITagService tagService;


		public RepositoryService()
			: this(
					new GitService(),
					new CacheService(),
					new CommitsService(),
					new BranchService(),
					new CommitBranchNameService(),
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
			IAheadBehindService aheadBehindService,
			ITagService tagService)
		{
			this.gitService = gitService;
			this.cacheService = cacheService;
			this.commitsService = commitsService;
			this.branchService = branchService;
			this.commitBranchNameService = commitBranchNameService;
			this.aheadBehindService = aheadBehindService;
			this.tagService = tagService;
		}


		public async Task<Repository> GetRepositoryAsync(bool useCache)
		{
			Timing t = new Timing();
			MRepository mRepository = null;
			if (useCache)
			{
				mRepository = await cacheService.TryGetAsync();
				t.Log("cacheService.TryGetAsync");
			}

			if (mRepository == null)
			{
				Log.Debug("No cached repository");
				mRepository = new MRepository();
				mRepository.Time = DateTime.Now;
				mRepository.CommitsFiles = new CommitsFiles();

				R<IGitRepo> gitRepo = await gitService.GetRepoAsync(null);
				//AddCommitsFilesAsync(mRepository.CommitsFiles, null).RunInBackground();

				t.Log("Got gitRepo");
				await UpdateAsync(mRepository, gitRepo.Value);
				t.Log("Updated mRepository");
				cacheService.CacheAsync(mRepository).RunInBackground();
			}

			Repository repository = ToRepository(mRepository);
			t.Log($"Created repository: {repository.Branches.Count} commits: {repository.Commits.Count}");

			return repository;
		}


		public async Task<Repository> UpdateRepositoryAsync(Repository repository)
		{
			Log.Debug($"Updating repository from time: {repository.Time}");

			//AddCommitsFilesAsync(repository.CommitsFiles, repository.Time).RunInBackground();

			MRepository mRepository = new MRepository();
			mRepository.Time = DateTime.Now;
			mRepository.CommitsFiles = repository.CommitsFiles;

			Timing t = new Timing();
			R<IGitRepo> gitRepo = await gitService.GetRepoAsync(null);
			t.Log("Got gitRepo");
			await UpdateAsync(mRepository, gitRepo.Value);
			t.Log("Updated mRepository");
			cacheService.CacheAsync(mRepository).RunInBackground();

			Repository updated = ToRepository(mRepository);
			t.Log($"Created repository: {updated.Branches.Count} commits: {updated.Commits.Count}");


			Log.Debug($"Updated to repository with time: {updated.Time}");

			return updated;
		}


		private Task UpdateAsync(MRepository mRepository, IGitRepo gitRepo)
		{
			return Task.Run(() =>
			{
				IReadOnlyList<GitCommit> gitCommits = gitRepo.GetAllCommts().ToList();
				IReadOnlyList<GitBranch> gitBranches = gitRepo.GetAllBranches();
				IReadOnlyList<SpecifiedBranchName> specifiedBranches = new SpecifiedBranchName[0];
				IReadOnlyList<GitTag> tags = gitRepo.GetAllTags();

				Update(
					mRepository,
					gitBranches,
					gitCommits,
					specifiedBranches,
					tags,
					gitRepo.CurrentBranch,
					gitRepo.CurrentCommit);
			});
		}


		private void Update(
			MRepository mRepository,
			IReadOnlyList<GitBranch> gitBranches,
			IReadOnlyList<GitCommit> gitCommits,
			IReadOnlyList<SpecifiedBranchName> specifiedBranches,
			IReadOnlyList<GitTag> tags,
			GitBranch currentBranch,
			GitCommit currentCommit)
		{
			Timing t = new Timing();
			IReadOnlyList<MCommit> commits = commitsService.AddCommits(gitCommits, mRepository);
			t.Log("Added commits");

			commitBranchNameService.SetCommitBranchNames(commits, specifiedBranches, mRepository);
			t.Log("Add commit branch names");

			tagService.AddTags(tags, mRepository);
			t.Log("Added tags");

			IReadOnlyList<MSubBranch> subBranches = branchService.AddSubBranches(
				gitBranches, mRepository, commits);
			t.Log("Add sub branches");

			commitBranchNameService.SetCommitBranchNames(subBranches, commits, mRepository);
			t.Log("Add commit branch names");

			IReadOnlyList<MSubBranch> multiBranches = branchService.AddMultiBranches(commits, subBranches, mRepository);
			t.Log("Add multi branches");
			Log.Debug($"Multi sub branches {multiBranches.Count} ({mRepository.SubBranches.Count})");
			commitBranchNameService.SetBranchCommits(multiBranches, mRepository);

			subBranches = subBranches.Concat(multiBranches).ToList();
			t.Log("Set multi branch commits");
			Log.Debug($"Unset commits after multi {commits.Count(c => !c.HasBranchName)}");

			branchService.SetBranchHierarchy(subBranches, mRepository);
			t.Log("SetBranchHierarchy");

			aheadBehindService.SetAheadBehind(mRepository);
			t.Log("SetAheadBehind");

			mRepository.CurrentBranchId = mRepository.Branches
				.First(b => b.IsActive && b.Name == currentBranch.Name).Id;
			mRepository.CurrentCommitId = mRepository.Commits[currentCommit.Id].Id;

			commits.Where(c => string.IsNullOrEmpty(c.BranchXName))
				.ForEach(c => Log.Warn($"   Unset {c} -> parent: {c.FirstParentId}"));
		}



		private Repository ToRepository(MRepository mRepository)
		{
			Timing t = new Timing();
			KeyedList<string, Branch> rBranches = new KeyedList<string, Branch>(b => b.Id);
			KeyedList<string, Commit> rCommits = new KeyedList<string, Commit>(c => c.Id);
			Branch currentBranch = null;
			Commit currentCommit = null;

			Repository repository = new Repository(
				mRepository.Time,
				new Lazy<IReadOnlyKeyedList<string, Branch>>(() => rBranches),
				new Lazy<IReadOnlyKeyedList<string, Commit>>(() => rCommits),
				new Lazy<Branch>(() => currentBranch),
				new Lazy<Commit>(() => currentCommit),
				mRepository.CommitsFiles);

			foreach (MCommit mCommit in mRepository.Commits)
			{
				Commit commit = Converter.ToCommit(repository, mCommit);
				rCommits.Add(commit);
				if (mCommit == mRepository.CurrentCommit)
				{
					currentCommit = commit;
				}
			}

			t.Log("Commits: " + rCommits.Count);

			foreach (MBranch mBranch in mRepository.Branches)
			{
				Branch branch = Converter.ToBranch(repository, mBranch);
				rBranches.Add(branch);

				if (mBranch == mRepository.CurrentBranch)
				{
					currentBranch = branch;
				}
			}

			t.Log("Branches: " + rBranches.Count);

			return repository;
		}


		private async Task AddCommitsFilesAsync(CommitsFiles commitsFiles, DateTime? dateTime)
		{
			Log.Debug("Getting commit files ...");

			Timing t = new Timing();

			int maxCount = 2000;
			int skip = 0;
			while (true)
			{
				List<CommitFiles> currentCommitsFiles = new List<CommitFiles>();
				R<IReadOnlyList<GitCommitFiles>> gitCommitFilesList =
					await gitService.GetCommitsFilesAsync(null, dateTime, maxCount, skip);
				skip += (maxCount - 10);

				if (gitCommitFilesList.IsFaulted)
				{
					Log.Warn($"Failed to get commits files {gitCommitFilesList.Error}");
					break;
				}

				if (gitCommitFilesList.Value.Count == 0)
				{
					break;
				}

				await Task.Run(() =>
				{
					foreach (GitCommitFiles gitCommitFiles in gitCommitFilesList.Value)
					{
						List<CommitFile> files = gitCommitFiles.Files.Select(Converter.ToCommitFile).ToList();
						CommitFiles commitFiles = new CommitFiles(gitCommitFiles.Id, files);

						if (commitsFiles.Add(commitFiles))
						{
							currentCommitsFiles.Add(commitFiles);
						}
					}
				});

				cacheService.CacheCommitFilesAsync(currentCommitsFiles).RunInBackground();
			}

			t.Log($"Total {commitsFiles.Count}");
		}
	}
}