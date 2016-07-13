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


				t.Log("Got gitRepo");
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


		private Task UpdateAsync(
			MRepository mRepository)
		{
			return Task.Run(() =>
			{
				Update(mRepository);
			});
		}


		private void Update(MRepository repository)
		{
			Log.Debug($"Updating repository");
			Timing t = new Timing();
			string gitRepositoryPath = repository.WorkingFolder;

			IReadOnlyList<GitSpecifiedNames> specifiedNames = gitService.GetSpecifiedNames(
				gitRepositoryPath);

			using (GitRepository gitRepository = gitService.OpenRepository(gitRepositoryPath))
			{
				commitsService.AddBranchCommits(gitRepository, repository);
				t.Log($"Added {repository.Commits.Count} commits referenced by active branches");

				AnalyzeBranchStructure(repository, specifiedNames, gitRepository);
				t.Log("AnalyzeBranchStructure");
			}
		}



		private void AnalyzeBranchStructure(
			MRepository repository,
			IReadOnlyList<GitSpecifiedNames> gitSpecifiedNames,
			GitRepository gitRepository)
		{
			Timing t = new Timing();

			repository.Commits.ForEach(c => c.Value.SubBranchId = null);
			repository.SubBranches.Clear();
			t.Log("Cleaned sub branches");

			commitBranchNameService.SetSpecifiedCommitBranchNames(gitSpecifiedNames, repository);
			t.Log($"Set {gitSpecifiedNames.Count} specified branch names");

			branchService.AddActiveBranches(gitRepository, repository);
			t.Log($"Added {repository.SubBranches.Count} active branches");

			commitBranchNameService.SetMasterBranchCommits(repository);
			t.Log("Set master branch names");

			branchService.AddInactiveBranches(repository);
			t.Log($"Added inactive branches, total: {repository.SubBranches.Count}");

			commitBranchNameService.SetBranchTipCommitsNames(repository);
			t.Log("Set branch tip commit branch names");

			commitBranchNameService.SetNeighborCommitNames(repository);
			t.Log("Set neighbor commit names");


			branchService.AddMissingInactiveBranches(repository);
			t.Log($"Added missing inactive branches, total: {repository.SubBranches.Count}");

			branchService.AddMultiBranches(repository);
			t.Log($"Added multi branches, total: {repository.SubBranches.Count}");

			Log.Debug($"Unset commits after multi {repository.Commits.Count(c => !c.Value.HasBranchName)}");

			branchHierarchyService.SetBranchHierarchy(repository);
			t.Log($"SetBranchHierarchy with {repository.Branches.Count} branches");

			aheadBehindService.SetAheadBehind(repository);
			t.Log("SetAheadBehind");

			tagService.AddTags(gitRepository, repository);
			t.Log("Added tags");

			repository.CurrentBranchId = repository.Branches
				.First(b => b.Value.IsActive && b.Value.Name == gitRepository.Head.Name).Value.Id;
			repository.CurrentCommitId = repository.Commits[gitRepository.Head.TipId].Id;

			repository.Commits.Where(c => string.IsNullOrEmpty(c.Value.BranchName))
				.ForEach(c => Log.Warn($"   Unset {c} -> parent: {c.Value.FirstParentId}"));
		}


		private static Repository ToRepository(MRepository mRepository)
		{
			Timing t = new Timing();
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
				mRepository.CommitsFiles);

			foreach (var mCommit in mRepository.Commits)
			{
				Commit commit = Converter.ToCommit(repository, mCommit.Value);
				rCommits.Add(commit);
				if (mCommit.Value == mRepository.CurrentCommit)
				{
					currentCommit = commit;
				}
			}

			t.Log("Commits: " + rCommits.Count);

			foreach (var mBranch in mRepository.Branches)
			{
				Branch branch = Converter.ToBranch(repository, mBranch.Value);
				rBranches.Add(branch);

				if (mBranch.Value == mRepository.CurrentBranch)
				{
					currentBranch = branch;
				}
			}

			t.Log("Branches: " + rBranches.Count);

			return repository;
		}
	}
}