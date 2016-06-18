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


		public RepositoryService()
			: this(new GitService(), new CacheService())
		{
		}

		public RepositoryService(IGitService gitService, ICacheService cacheService)
		{
			this.gitService = gitService;
			this.cacheService = cacheService;
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

				AddCommitsFilesAsync(mRepository.CommitsFiles, null).RunInBackground();

				R<IGitRepo> gitRepo = await gitService.GetRepoAsync(null);
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

			AddCommitsFilesAsync(repository.CommitsFiles, repository.Time).RunInBackground();

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
				IReadOnlyList<SpecifiedBranch> specifiedBranches = new SpecifiedBranch[0];

				Update(
					mRepository,
					gitBranches,
					gitCommits,
					specifiedBranches,
					gitRepo.CurrentBranch,
					gitRepo.CurrentCommit);
			});
		}


		private void Update(
			MRepository mRepository,
			IReadOnlyList<GitBranch> gitBranches,
			IReadOnlyList<GitCommit> gitCommits,
			IReadOnlyList<SpecifiedBranch> specifiedBranches,
			GitBranch currentBranch,
			GitCommit currentCommit)
		{
			Timing t = new Timing();
			IReadOnlyList<MCommit> commits = AddCommits(gitCommits, specifiedBranches, mRepository);
			t.Log("Added commits");

			IReadOnlyList<MSubBranch> subBranches = AddSubBranches(gitBranches, mRepository, commits);
			t.Log("Add sub branches");

			SetCommitBranchNames(subBranches, commits, mRepository);
			t.Log("Add commit branch names");

			IReadOnlyList<MSubBranch> multiBranches = AddMultiBranches(commits, subBranches, mRepository);
			t.Log("Add multi branches");
			Log.Debug($"Multi sub branches {multiBranches.Count} ({mRepository.SubBranches.Count})");
			SetBranchCommits(multiBranches, mRepository);

			subBranches = subBranches.Concat(multiBranches).ToList();
			t.Log("Set multi branch commits");
			Log.Debug($"Unset commits after multi {commits.Count(c => !c.HasBranchName)}");

			SetBranchHierarchy(subBranches, mRepository);
			t.Log("SetBranchHierarchy");

			SetAheadBehind(mRepository);
			t.Log("SetAheadBehind");

			mRepository.CurrentBranchId = mRepository.Branches
				.First(b => b.IsActive && b.Name == currentBranch.Name).Id;
			mRepository.CurrentCommitId = mRepository.Commits[currentCommit.Id].Id;

			commits.Where(c => string.IsNullOrEmpty(c.BranchXName))
				.ForEach(c => Log.Warn($"   Unset {c} -> parent: {c.FirstParentId}"));
		}


		private void SetAheadBehind(MRepository repository)
		{
			var bothLocalAndRemotebranches = repository.SubBranches.Where(b => b.IsActive)
				.GroupBy(b => b.BranchId)
				.Where(g => g.Count() == 2 && g.Any(b => !b.IsRemote) && g.Any(b => b.IsRemote))
				.Select(g => repository.Branches.First(b => b.Id == g.Key));

			bothLocalAndRemotebranches.ForEach(b => b.IsLocalAndRemote = true);

			Timing t = new Timing();
			var localBranches = repository.SubBranches.Where(b => b.IsActive && !b.IsRemote);
			localBranches.ForEach(branch => MarkIsLocalAhead(branch.LatestCommit));
			t.Log("Local commits");

			var remoteBranches = repository.SubBranches.Where(b => b.IsActive && b.IsRemote);
			remoteBranches.ForEach(branch => MarkIsRemoteAhead(branch.LatestCommit));
			t.Log("Remote commits");

			foreach (MBranch branch in repository.Branches)
			{
				int localAheadCount = 0;
				int remoteAheadCount = 0;
				foreach (MCommit commit in branch.Commits)
				{
					if (commit.IsLocalAhead)
					{
						localAheadCount++;
					}
					else if (commit.IsRemoteAhead)
					{
						remoteAheadCount++;
					}
				}

				branch.LocalAheadCount = localAheadCount;
				branch.RemoteAheadCount = remoteAheadCount;
			}

			t.Log("Summery of local and remote commits");
		}


		private void MarkIsLocalAhead(MCommit commit)
		{
			if (!commit.IsLocalAheadMarker)
			{
				commit.IsLocalAheadMarker = true;

				foreach (MCommit parent in commit.Parents)
				{
					MarkIsLocalAhead(parent);
				}
			}
		}

		private void MarkIsRemoteAhead(MCommit commit)
		{
			if (!commit.IsRemoteAheadMarker)
			{
				commit.IsRemoteAheadMarker = true;

				foreach (MCommit parent in commit.Parents)
				{
					MarkIsRemoteAhead(parent);
				}
			}
		}


		private static void SetBranchHierarchy(
			IReadOnlyList<MSubBranch> subBranches, MRepository mRepository)
		{
			SetParentCommitId(subBranches);
			GroupSubBranches(subBranches);
			SetBranchHierarchy(mRepository.Branches);
		}


		private IReadOnlyList<MSubBranch> AddSubBranches(
			IReadOnlyList<GitBranch> gitBranches, MRepository mRepository, IReadOnlyList<MCommit> commits)
		{
			Timing t = new Timing();
			IReadOnlyList<MSubBranch> activeBranches = AddActiveBranches(gitBranches, mRepository);
			t.Log("Added branches");
			Log.Debug($"Active sub branches {activeBranches.Count} ({mRepository.SubBranches.Count})");

			IReadOnlyList<MSubBranch> inactiveBranches = AddInactiveBranches(commits, mRepository);
			IReadOnlyList<MSubBranch> branches = activeBranches.Concat(inactiveBranches).ToList();
			t.Log("Inactive subbranches");
			Log.Debug($"Inactive sub branches {inactiveBranches.Count} ({mRepository.SubBranches.Count})");
			//branches2.ForEach(b => Log.Debug($"   Branch {b}"));
			return branches;
		}


		private void SetCommitBranchNames(
			IReadOnlyList<MSubBranch> branches,
			IReadOnlyList<MCommit> commits,
			MRepository mRepository)
		{
			Timing t = new Timing();
			SetMasterBranchCommits(branches, mRepository);
			t.Log("Set master branch commits");

			SetBranchCommits(branches, mRepository);
			t.Log("Set branch commits");

			SetEmptyParentCommits(commits);
			t.Log("Set empty parent commits");

			SetBranchCommitsOfParents(commits);
			t.Log("Set same branch name as parent with name");
		}


		private IReadOnlyList<MCommit> AddCommits(
			IReadOnlyList<GitCommit> gitCommits,
			IReadOnlyList<SpecifiedBranch> specifiedBranches,
			MRepository mRepository)
		{
			Timing t = new Timing();
			IReadOnlyList<MCommit> commits = AddCommits(gitCommits, mRepository);
			t.Log("added commits");

			SetChildren(commits);
			t.Log("Set children");

			SetSpecifiedCommitBranchNames(specifiedBranches, mRepository);
			t.Log("Set specified branch names");

			SetSubjectCommitBranchNames(commits, mRepository);
			t.Log("Parse subject branch names");
			return commits;
		}


		private Repository ToRepository(MRepository mRepository)
		{
			Timing t = new Timing();
			KeyedList<string, Branch> rBranches = new KeyedList<string, Branch>(b => b.Id);
			KeyedList<string, Commit> rCommits = new KeyedList<string, Commit>(c => c.Id);
			Branch currentBranch = null;
			Commit currentCommit = null;

			//CommitFiles commitFiles = new CommitFiles();
			//commitFiles.AddFiles(mRepository.CommitsFilesTask);

			Repository repository = new Repository(
				mRepository.Time,
				new Lazy<IReadOnlyKeyedList<string, Branch>>(() => rBranches),
				new Lazy<IReadOnlyKeyedList<string, Commit>>(() => rCommits),
				new Lazy<Branch>(() => currentBranch),
				new Lazy<Commit>(() => currentCommit),
				mRepository.CommitsFiles);

			foreach (MCommit mCommit in mRepository.Commits)
			{
				Commit commit = ToCommit(repository, mCommit);
				rCommits.Add(commit);
				if (mCommit == mRepository.CurrentCommit)
				{
					currentCommit = commit;
				}
			}

			t.Log("Commits: " + rCommits.Count);

			foreach (MBranch mBranch in mRepository.Branches)
			{
				Branch branch = ToBranch(repository, mBranch);
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
			//await Task.Yield();
			//return new Dictionary<string, IEnumerable<CommitFile>>();
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
						List<CommitFile> files = gitCommitFiles.Files.Select(ToCommitFile).ToList();
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


		private static CommitFile ToCommitFile(GitFile gitFile)
		{
			return new CommitFile(gitFile.File, "?");
		}


		private static Branch ToBranch(Repository repository, MBranch branch)
		{
			return new Branch(
				repository,
				branch.Id,
				branch.Name,
				branch.LatestCommitId,
				branch.FirstCommitId,
				branch.ParentCommitId,
				branch.Commits.Select(c => c.Id).ToList(),
				branch.ParentBranchId,
				branch.IsActive,
				branch.IsMultiBranch,
				branch.LocalAheadCount,
				branch.RemoteAheadCount);
		}


		private static Commit ToCommit(Repository repository, MCommit commit)
		{
			return new Commit(
				repository,
				commit.Id,
				commit.ShortId,
				commit.Subject,
				commit.Author,
				commit.AuthorDate,
				commit.CommitDate,
				commit.ParentIds.ToList(),
				commit.ChildIds.ToList(),
				commit.BranchId,
				commit.IsLocalAhead,
				commit.IsRemoteAhead);
		}


		private static void SetBranchHierarchy(IReadOnlyList<MBranch> branches)
		{
			foreach (MBranch xBranch in branches)
			{
				if (xBranch.ParentCommitId != null && xBranch.ParentCommit.BranchId != xBranch.Id)
				{
					xBranch.ParentBranchId = xBranch.ParentCommit.BranchId;

					MBranch parentBranch = xBranch.ParentBranch;
					if (!parentBranch.ChildBranches.Contains(xBranch))
					{
						parentBranch.ChildBrancheIds.Add(xBranch.Id);
					}
				}
				else
				{
					Log.Debug($"Branch {xBranch} has no parent branch");
				}
			}

			//foreach (XBranch xBranch in branches.Where(b => b.ParentBranchId == null))
			//{
			//	LogBranchHierarchy(xBranch, 0);
			//}
		}


		private void LogBranchHierarchy(MBranch mBranch, int indent)
		{
			string indentText = new string(' ', indent);
			Log.Debug($"{indentText}{mBranch}");

			foreach (MBranch childBranch in mBranch.ChildBranches.OrderBy(b => b.Name))
			{
				LogBranchHierarchy(childBranch, indent + 3);
			}
		}


		private static void SetParentCommitId(IReadOnlyList<MSubBranch> subBranches)
		{
			foreach (MSubBranch subBranch in subBranches)
			{
				MCommit LatestCommit = subBranch.LatestCommit;

				IEnumerable<MCommit> commits = subBranch.LatestCommit.FirstAncestors()
					.TakeWhile(c => c.BranchXName == subBranch.Name);

				if (commits.Any())
				{
					MCommit firstCommit = commits.Last();
					subBranch.FirstCommitId = firstCommit.Id;
					subBranch.ParentCommitId = firstCommit.FirstParentId;
				}
				else
				{
					if (LatestCommit.BranchXName != null)
					{
						subBranch.FirstCommitId = LatestCommit.Id;
						subBranch.ParentCommitId = LatestCommit.FirstParentId;
					}
					else
					{
						Log.Warn($"Branch with no commits {subBranch}");
					}
				}
			}
		}

		private static void GroupSubBranches(IReadOnlyList<MSubBranch> branches)
		{
			var groupedOnName = branches.GroupBy(b => b.Name);

			foreach (var groupByName in groupedOnName)
			{
				var groupedByParentCommitId = groupByName.GroupBy(b => b.ParentCommitId);

				foreach (var groupByBranch in groupedByParentCommitId)
				{
					MSubBranch subBranch = groupByBranch.First();
					MBranch mBranch = new MBranch
					{
						Repository = subBranch.Repository,
						Name = subBranch.Name,
						IsMultiBranch = subBranch.IsMultiBranch,
						IsActive = subBranch.IsActive,
						IsAnonymous = subBranch.IsAnonymous,
						ParentCommitId = subBranch.ParentCommitId
					};

					mBranch.Id = subBranch.Name + "-" + subBranch.ParentCommitId;

					mBranch.SubBrancheIds.AddRange(groupByBranch.Select(b => b.Id));
					mBranch.SubBranches.ForEach(b => b.BranchId = mBranch.Id);

					mBranch.CommitIds.AddRange(
						groupByBranch
							.SelectMany(branch =>
								new[] { branch.LatestCommit }
									.Where(c => c.SubBranchId == branch.Id && c.Id != branch.ParentCommitId)
								.Concat(
									branch.LatestCommit
										.FirstAncestors()
										.TakeWhile(c => c.SubBranchId == branch.Id && c.Id != branch.ParentCommitId)))
								.Distinct()
							.OrderByDescending(c => c.CommitDate)
							.Select(c => c.Id));

					if (mBranch.Commits.Any(c => c.BranchId != null))
					{
						Log.Error($"Commits belong to multiple branches {mBranch}");
					}

					mBranch.Commits.ForEach(c => c.BranchId = mBranch.Id);

					mBranch.LatestCommitId = mBranch.Commits.Any()
						? mBranch.Commits.First().Id : mBranch.ParentCommitId;
					mBranch.FirstCommitId = mBranch.Commits.Any()
					? mBranch.Commits.Last().Id : mBranch.ParentCommitId;

					mBranch.Repository.Branches.Add(mBranch);
				}
			}
		}


		private void SetEmptyParentCommits(IReadOnlyList<MCommit> commits)
		{
			// All commits, which do have a name, but first parent commit does not have a name
			IEnumerable<MCommit> commitsWithBranchName =
				commits.Where(commit =>
					commit.HasBranchName
					&& commit.HasFirstParent
					&& !commit.FirstParent.HasBranchName);

			foreach (MCommit xCommit in commitsWithBranchName)
			{
				string branchName = xCommit.BranchXName;
				string subBranchId = xCommit.SubBranchId;

				MCommit last = xCommit;
				bool isFound = false;
				foreach (MCommit current in xCommit.FirstAncestors())
				{
					string currentBranchName = GetBranchName(current);

					if (current.HasBranchName && current.BranchXName != branchName)
					{
						// found commit with branch name already set 
						break;
					}

					//if (string.IsNullOrEmpty(currentBranchName) || currentBranchName == branchName)
					//{

					//}

					if (currentBranchName == branchName)
					{
						isFound = true;
						last = current;
					}
				}

				if (isFound)
				{
					foreach (MCommit current in xCommit.FirstAncestors())
					{
						current.BranchXName = branchName;
						current.SubBranchId = subBranchId;

						if (current == last)
						{
							break;
						}
					}
				}
			}
		}


		private static void SetBranchCommitsOfParents(IReadOnlyList<MCommit> commits)
		{
			IEnumerable<MCommit> commitsWithBranchName =
				commits.Where(commit =>
					commit.HasBranchName
					&& commit.HasFirstParent
					&& !commit.FirstParent.HasBranchName);

			foreach (MCommit xCommit in commitsWithBranchName)
			{
				string branchName = xCommit.BranchXName;
				string subBranchId = xCommit.SubBranchId;

				foreach (MCommit current in xCommit.FirstAncestors()
					.TakeWhile(c => c.FirstChildIds.Count <= 1 && !c.HasBranchName))
				{
					current.BranchXName = branchName;
					current.SubBranchId = subBranchId;
				}
			}
		}


		private IReadOnlyList<MSubBranch> AddMultiBranches(
			IReadOnlyList<MCommit> commits, IReadOnlyList<MSubBranch> branches, MRepository xmodel)
		{
			IEnumerable<MCommit> roots =
				commits.Where(c =>
				string.IsNullOrEmpty(c.BranchXName)
				&& c.FirstChildIds.Count > 1);

			// The commits where multiple branches are starting and the commits has no branch name
			IEnumerable<MCommit> roots2 = branches
				.GroupBy(b => b.LatestCommitId)
				.Where(group => group.Count() > 1)
				.Select(group => xmodel.Commits[group.Key])
				.Where(c => string.IsNullOrEmpty(c.BranchXName));

			roots = roots.Concat(roots2);

			List<MSubBranch> multiBranches = new List<MSubBranch>();
			foreach (MCommit root in roots)
			{
				string branchName = "Multibranch_" + root.ShortId;

				if (root.Children.Any() &&
						root.Children.All(c => c.HasBranchName && c.BranchXName == root.Children.ElementAt(0).BranchXName))
				{
					// All children have the same branch name thus this branch is just a continuation of them
					branchName = root.Children.ElementAt(0).BranchXName;
				}


				MSubBranch subBranch = new MSubBranch
				{
					Repository = xmodel,
					Id = Guid.NewGuid().ToString(),
					Name = branchName,
					LatestCommitId = root.Id,
					IsMultiBranch = true,
					IsActive = false,
					IsAnonymous = true
				};

				xmodel.SubBranches.Add(subBranch);
				multiBranches.Add(subBranch);
			}

			return multiBranches;
		}


		private IReadOnlyList<MSubBranch> AddInactiveBranches(
			IReadOnlyList<MCommit> commits, MRepository mRepository)
		{
			List<MSubBranch> branches = new List<MSubBranch>();

			// Commits which has no child, which has this commit as a first parent, i.e. it is the 
			// top of a branch and there is no existing branch at this commit
			IEnumerable<MCommit> topCommits = commits.Where(commit =>
				!commit.FirstChildIds.Any()
				&& !mRepository.SubBranches.Any(b => b.LatestCommitId == commit.Id));

			IEnumerable<MCommit> pullMergeTopCommits = commits
				.Where(commit =>
					commit.HasSecondParent
					&& commit.MergeSourceBranchNameFromSubject != null
					&& commit.MergeSourceBranchNameFromSubject == commit.MergeTargetBranchNameFromSubject)
				.Select(c => c.SecondParent);

			topCommits = topCommits.Concat(pullMergeTopCommits).Distinct();


			foreach (MCommit xCommit in topCommits)
			{
				MSubBranch subBranch = new MSubBranch
				{
					Repository = mRepository,
					Id = Guid.NewGuid().ToString(),
					LatestCommitId = xCommit.Id,
					IsMultiBranch = false,
					IsActive = false
				};

				string branchName = TryFindBranchName(xCommit);
				if (string.IsNullOrEmpty(branchName))
				{
					branchName = "Branch_" + xCommit.ShortId;
					subBranch.IsAnonymous = true;
				}

				subBranch.Name = branchName;

				mRepository.SubBranches.Add(subBranch);
				branches.Add(subBranch);
			}


			return branches;
		}


		private string TryFindBranchName(MCommit mCommit)
		{
			string branchName = GetBranchName(mCommit);

			if (branchName == null)
			{
				int count = 0;
				// Could not find a branch name from the commit, lets try it ancestors
				foreach (MCommit commit in mCommit.FirstAncestors()
					.TakeWhile(c => c.HasSingleFirstChild))
				{
					count++;
					string name = GetBranchName(commit);
					if (name != null)
					{
						return name;
					}
				}
			}

			return branchName;
		}


		private void SetBranchCommits(IReadOnlyList<MSubBranch> branches, MRepository xmodel)
		{
			foreach (MSubBranch xBranch in branches.ToList())
			{
				string id = xBranch.LatestCommitId;
				SetBranchName(xmodel, id, xBranch);
			}
		}


		private void SetBranchName(MRepository xmodel, string id, MSubBranch subBranch)
		{
			if (string.IsNullOrEmpty(id))
			{
				return;
			}

			if (xmodel.Commits[id].ShortId == "afe62f")
			{

			}

			foreach (MSubBranch b in xmodel.SubBranches)
			{
				if (b.Name != subBranch.Name
					&& !(subBranch.IsActive && !b.IsActive)
					&& !(subBranch.IsMultiBranch)
					&& (b.LatestCommitId == id))
				{
					MCommit c = xmodel.Commits[id];
					//Log.Warn($"Commit {c} in branch {xBranch} same as other branch {b}");
					return;
				}
			}

			List<MCommit> pullmerges = new List<MCommit>();

			string currentId = id;
			while (true)
			{
				if (currentId == null)
				{
					break;
				}

				MCommit mCommit = xmodel.Commits[currentId];

				if (!string.IsNullOrEmpty(mCommit.BranchXName))
				{
					break;
				}

				if (IsPullMergeCommit(mCommit, subBranch))
				{
					pullmerges.Add(mCommit);

				}

				if (GetBranchName(mCommit) != subBranch.Name &&
					!(subBranch.IsMultiBranch && currentId == id))
				{
					// for multi branches, first commit is a branch root
					if (mCommit.ChildIds.Count > 1)
					{
						if (0 != mCommit.FirstChildren.Count(child => GetBranchName(child) != subBranch.Name))
						{
							//Log.Warn($"Found commit which belongs to multiple different branches: {xCommit}");
							break;
						}

						if (0 != xmodel.SubBranches.Count(b => b != subBranch && b.LatestCommit == mCommit))
						{
							break;
						}
					}
				}

				mCommit.BranchXName = subBranch.Name;
				mCommit.SubBranchId = subBranch.Id;

				currentId = mCommit.FirstParentId;
			}

			foreach (MCommit xCommit in pullmerges)
			{
				//SetBranchName(xmodel, xCommit.SecondParentId, subBranch);

				//RemovePullMergeBranch(xmodel, xBranch, xCommit.SecondParentId);
			}
		}


		private bool IsPullMergeCommit(MCommit mCommit, MSubBranch subBranch)
		{
			return
				mCommit.HasSecondParent
				&& (mCommit.MergeSourceBranchNameFromSubject == subBranch.Name
					|| GetBranchName(mCommit.SecondParent) == subBranch.Name);
		}


		private static string GetBranchName(MCommit mCommit)
		{
			if (!string.IsNullOrEmpty(mCommit.BranchXName))
			{
				return mCommit.BranchXName;
			}
			else if (!string.IsNullOrEmpty(mCommit.BranchNameSpecified))
			{
				return mCommit.BranchNameSpecified;
			}
			else if (!string.IsNullOrEmpty(mCommit.BranchNameFromSubject))
			{
				return mCommit.BranchNameFromSubject;
			}

			return null;
		}


		private void SetMasterBranchCommits(IReadOnlyList<MSubBranch> branches, MRepository xmodel)
		{
			// Local master
			MSubBranch master = branches.FirstOrDefault(b => b.Name == "master" && !b.IsRemote);
			if (master != null)
			{
				SetBranchNameWithPriority(xmodel, master.LatestCommitId, master);
			}

			// Remote master
			master = branches.FirstOrDefault(b => b.Name == "master" && b.IsRemote);
			if (master != null)
			{
				SetBranchNameWithPriority(xmodel, master.LatestCommitId, master);
			}
		}


		private void SetBranchNameWithPriority(MRepository xmodel, string id, MSubBranch subBranch)
		{
			List<MCommit> pullmerges = new List<MCommit>();

			while (true)
			{
				if (id == null)
				{
					break;
				}

				MCommit mCommit = xmodel.Commits[id];

				if (mCommit.BranchXName == subBranch.Name)
				{
					break;
				}

				if (IsPullMergeCommit(mCommit, subBranch))
				{
					pullmerges.Add(mCommit);
				}

				if (!string.IsNullOrEmpty(mCommit.BranchNameFromSubject) &&
					mCommit.BranchNameFromSubject != subBranch.Name)
				{
					//Log.Debug($"Setting different name '{xBranch.Name}'!='{xCommit.BranchNameFromSubject}'");
				}

				mCommit.BranchXName = subBranch.Name;
				mCommit.SubBranchId = subBranch.Id;


				id = mCommit.FirstParentId;
			}

			foreach (MCommit xCommit in pullmerges)
			{
				//SetBranchNameWithPriority(xmodel, xCommit.SecondParentId, subBranch);
				//RemovePullMergeBranch(xmodel, xBranch, xCommit.SecondParentId);
			}
		}


		private void SetChildren(IReadOnlyList<MCommit> commits)
		{
			foreach (MCommit xCommit in commits)
			{
				bool isFirstParent = true;
				foreach (MCommit parent in xCommit.Parents)
				{
					if (!parent.Children.Contains(xCommit))
					{
						parent.ChildIds.Add(xCommit.Id);
					}

					if (isFirstParent)
					{
						isFirstParent = false;
						if (!parent.FirstChildren.Contains(xCommit))
						{
							parent.FirstChildIds.Add(xCommit.Id);
						}
					}
				}
			}
		}


		private void SetSpecifiedCommitBranchNames(
			IReadOnlyList<SpecifiedBranch> commitBranches,
			MRepository xmodel)
		{
			foreach (SpecifiedBranch commitBranch in commitBranches)
			{
				MCommit mCommit;
				if (xmodel.Commits.TryGetValue(commitBranch.CommitId, out mCommit))
				{
					mCommit.BranchNameSpecified = commitBranch.BranchName;
					mCommit.BranchXName = commitBranch.BranchName;
					mCommit.SubBranchId = commitBranch.SubBranchId;
				}
			}
		}


		private void SetSubjectCommitBranchNames(
			IReadOnlyList<MCommit> commits,
			MRepository xmodel)
		{
			foreach (MCommit xCommit in commits)
			{
				xCommit.BranchNameFromSubject = TryExtractBranchNameFromSubject(xCommit, xmodel);
			}
		}



		private IReadOnlyList<MCommit> AddCommits(IReadOnlyList<GitCommit> gitCommits, MRepository xmodel)
		{
			return gitCommits.Select(
				c =>
				{
					MCommit mCommit = ToCommit(c, xmodel);
					xmodel.Commits.Add(mCommit);
					return mCommit;
				})
				.ToList();
		}


		private IReadOnlyList<MSubBranch> AddActiveBranches(
			IReadOnlyList<GitBranch> gitBranches, MRepository xmodel)
		{
			return gitBranches.Select(gitBranch =>
			{
				MSubBranch subBranch = ToBranch(gitBranch, xmodel);
				xmodel.SubBranches.Add(subBranch);
				return subBranch;
			})
			.ToList();
		}


		private MSubBranch ToBranch(GitBranch gitBranch, MRepository mRepository)
		{
			string latestCommitId = gitBranch.LatestCommitId;

			return new MSubBranch
			{
				Repository = mRepository,
				Id = Guid.NewGuid().ToString(),
				Name = gitBranch.Name,
				LatestCommitId = latestCommitId,
				IsMultiBranch = false,
				IsActive = true,
				IsRemote = gitBranch.IsRemote
			};
		}


		private MCommit ToCommit(GitCommit gitCommit, MRepository mRepository)
		{
			MergeBranchNames branchNames = ParseMergeNamesFromSubject(gitCommit);

			return new MCommit
			{
				Repository = mRepository,
				Id = gitCommit.Id,
				ShortId = gitCommit.ShortId,
				Subject = gitCommit.Subject,
				Author = gitCommit.Author,
				AuthorDate = gitCommit.AuthorDate,
				CommitDate = gitCommit.CommitDate,
				ParentIds = gitCommit.ParentIds.ToList(),
				MergeSourceBranchNameFromSubject = branchNames.SourceBranchName,
				MergeTargetBranchNameFromSubject = branchNames.TargetBranchName,
			};
		}


		private string TryExtractBranchNameFromSubject(MCommit mCommit, MRepository mRepository)
		{
			if (mCommit.SecondParentId != null)
			{
				// This is a merge commit, and the subject might contain the target (this current) branch 
				// name in the subject like e.g. "Merge <source-branch> into <target-branch>"
				string branchName = mCommit.MergeTargetBranchNameFromSubject;
				if (branchName != null)
				{
					return branchName;
				}
			}

			// If a child of this commit is a merge commit merged from this commit, lets try to get
			// the source branch name of that commit. I.e. a child commit might have a subject like
			// e.g. "Merge <source-branch> ..." That source branch would thus be the name of the branch
			// of this commit.
			foreach (string childId in mCommit.ChildIds)
			{
				MCommit child = mRepository.Commits[childId];
				if (child.SecondParentId == mCommit.Id
					&& !string.IsNullOrEmpty(child.MergeSourceBranchNameFromSubject))
				{
					return child.MergeSourceBranchNameFromSubject;
				}
			}

			return null;
		}

		private MergeBranchNames ParseMergeNamesFromSubject(GitCommit gitCommit)
		{
			if (gitCommit.ParentIds.Count <= 1)
			{
				// This is no merge commit, i.e. no branch names to parse
				return BranchNameParser.NoMerge;
			}

			return BranchNameParser.ParseBranchNamesFromSubject(gitCommit.Subject);
		}
	}


	internal class SpecifiedBranch
	{
		public string CommitId { get; set; }
		public string BranchName { get; set; }
		public string SubBranchId { get; set; }


		public SpecifiedBranch(string commitId, string branchName, string subBranchId)
		{
			CommitId = commitId;
			BranchName = branchName;
			SubBranchId = subBranchId;
		}
	}
}