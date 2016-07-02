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


		public async Task<Repository> UpdateRepositoryAsync(Repository sourcerepository)
		{
			Log.Debug($"Updating repository from time: {sourcerepository.Time}");

			//AddCommitsFilesAsync(repository.CommitsFiles, repository.Time).RunInBackground();

			MRepository mRepository = new MRepository();
			mRepository.Time = DateTime.Now;
			mRepository.CommitsFiles = sourcerepository.CommitsFiles;

			Timing t = new Timing();
			R<IGitRepo> gitRepo = await gitService.GetRepoAsync(null);
			t.Log($"Got gitRepo for {Environment.CurrentDirectory}");
			await UpdateAsync(mRepository, gitRepo.Value);
			t.Log("Updated mRepository");
			cacheService.CacheAsync(mRepository).RunInBackground();

			Repository repository = ToRepository(mRepository);
			t.Log($"Repository {repository.Branches.Count} branches, {repository.Commits.Count} commits");
			Log.Debug($"Updated to repository with time: {repository.Time}");

			return repository;
		}


		public Task SetSpecifiedCommitBranchAsync(string commitId, string branchName)
		{
			return gitService.SetSpecifiedCommitBranchAsync(commitId, branchName);
		}


		private Task UpdateAsync(MRepository mRepository, IGitRepo gitRepo)
		{
			return Task.Run(() =>
			{
				IReadOnlyList<GitCommit> gitCommits = gitRepo.GetAllCommts().ToList();
				IReadOnlyList<GitBranch> gitBranches = gitRepo.GetAllBranches();
				IReadOnlyList<GitSpecifiedNames> specifiedBranches = gitRepo.GetSpecifiedNameses();
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
			MRepository repository,
			IReadOnlyList<GitBranch> gitBranches,
			IReadOnlyList<GitCommit> gitCommits,
			IReadOnlyList<GitSpecifiedNames> gitSpecifiedNames,
			IReadOnlyList<GitTag> gitTags,
			GitBranch currentBranch,
			GitCommit currentCommit)
		{
			Timing t = new Timing();
			IReadOnlyList<MCommit> commits = commitsService.AddCommits(gitCommits, repository);
			t.Log($"Added {commits.Count} commits");
			
			commitBranchNameService.SetSpecifiedCommitBranchNames(gitSpecifiedNames, repository);
			t.Log($"Set {gitSpecifiedNames.Count} specified branch names");

			commitBranchNameService.SetPullMergeCommitBranchNames(commits);
			t.Log("Set pull merge commit branch names");

			commitBranchNameService.SetSubjectCommitBranchNames(commits, repository);
			t.Log("Set commit subject branch names");


			IReadOnlyList<MSubBranch> activeBranches = branchService.AddActiveBranches(gitBranches, repository);
			t.Log($"Added {activeBranches.Count} active branches");

			IReadOnlyList<MSubBranch> inactiveBranches = branchService.AddInactiveBranches(commits, repository);
			t.Log($"Added {inactiveBranches.Count} inactive branches");

			IReadOnlyList<MSubBranch> subBranches = activeBranches.Concat(inactiveBranches).ToList();
			
			commitBranchNameService.SetMasterBranchCommits(subBranches, repository);
			t.Log("Set master branch names");		

			commitBranchNameService.SetBranchTipCommitsNames(subBranches, repository);
			t.Log("Set branch tip commit branch names");	

			commitBranchNameService.SetNeighborCommitNames(commits);
			t.Log("Set neighbor commit names");

		
			IReadOnlyList<MSubBranch> missingInactiveBranches = branchService.AddMissingInactiveBranches(
				commits, repository);
			t.Log($"Added {missingInactiveBranches.Count} missing inactive branches");
			subBranches = subBranches.Concat(missingInactiveBranches).ToList();
			
			IReadOnlyList<MSubBranch> multiBranches = branchService.AddMultiBranches(commits, repository);
			t.Log($"Added {multiBranches.Count(B => B.IsMultiBranch)} multi branches");

			Log.Debug($"Unset commits after multi {commits.Count(c => !c.HasBranchName)}");
			Log.Debug($"Unset commits id after multi {commits.Count(c => c.SubBranchId == null)}");

			subBranches = subBranches.Concat(multiBranches).ToList();
			t.Log($"Total {subBranches.Count} sub branches");

		

			branchHierarchyService.SetBranchHierarchy(subBranches, repository);
			t.Log($"SetBranchHierarchy with {repository.Branches.Count} branches");

			aheadBehindService.SetAheadBehind(repository);
			t.Log("SetAheadBehind");

			tagService.AddTags(gitTags, repository);
			t.Log("Added tags");

			repository.CurrentBranchId = repository.Branches
				.First(b => b.IsActive && b.Name == currentBranch.Name).Id;
			repository.CurrentCommitId = repository.Commits[currentCommit.Id].Id;

			commits.Where(c => string.IsNullOrEmpty(c.BranchXName))
				.ForEach(c => Log.Warn($"   Unset {c} -> parent: {c.FirstParentId}"));

			t.Log("Total time");
		}



		private static Repository ToRepository(MRepository mRepository)
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