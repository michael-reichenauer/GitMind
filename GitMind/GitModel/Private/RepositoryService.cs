using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class RepositoryService : IRepositoryService
	{
		private readonly IGitService gitService;


		public RepositoryService()
			: this( new GitService())
		{			
		}

		public RepositoryService(IGitService gitService)
		{
			this.gitService = gitService;
		}


		public async Task<Repository> GetRepositoryAsync()
		{
			R<IGitRepo> gitRepo = await gitService.GetRepoAsync(null);

			return await GetRepositoryAsync(gitRepo.Value);
		}


		public Task<Repository> GetRepositoryAsync(IGitRepo gitRepo)
		{
			return Task.Run(() =>
			{
				IReadOnlyList<GitCommit> gitCommits = gitRepo.GetAllCommts().ToList();
				IReadOnlyList<GitBranch> gitBranches = gitRepo.GetAllBranches();
				IReadOnlyList<SpecifiedBranch> specifiedBranches = new SpecifiedBranch[0];

				MRepository mRepository = new MRepository();

				return GetRepository(
					mRepository, 
					gitBranches, 
					gitCommits, 
					specifiedBranches, 
					gitRepo.CurrentBranch, 
					gitRepo.CurrentCommit);
			});
		}


		private Repository GetRepository(
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

			SetBranchHierarchy(subBranches, mRepository);
			t.Log("SetBranchHierarchy");

			mRepository.CurrentBranch = mRepository.Branches
				.First(b => b.IsActive && b.Name == currentBranch.Name);
			mRepository.CurrentCommit = mRepository.Commits[currentCommit.Id];

			Repository repository = ToRepository(mRepository);
			t.Log($"Branches: {repository.Branches.Count} commits: {repository.Commits.Count}");

			Log.Debug($"Unset commits after multi {commits.Count(c => !c.HasBranchName)}");
			commits.Where(c => string.IsNullOrEmpty(c.BranchName))
				.ForEach(c => Log.Warn($"   Unset {c} -> parent: {c.FirstParentId}"));

			return repository;
		}


		private static void SetBranchHierarchy(
			IReadOnlyList<MSubBranch> subBranches, MRepository mRepository)
		{
			SetParentCommitId(subBranches);
			GroupSubBranches(subBranches);
			SetBranchHierarchy(mRepository.Branches);
		}


		private IReadOnlyList<MSubBranch> AddSubBranches(
			IReadOnlyList<GitBranch> gitBranches, MRepository mRepository,  IReadOnlyList<MCommit> commits)
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


		private static Repository ToRepository(MRepository mRepository)
		{
			Timing t = new Timing();
			KeyedList<string, Branch> rBranches = new KeyedList<string, Branch>(b => b.Id);
			KeyedList<string, Commit> rCommits = new KeyedList<string, Commit>(c => c.Id);
			Branch currentBranch = null;
			Commit currentCommit = null;

			Repository repository = new Repository(
				new Lazy<IReadOnlyKeyedList<string, Branch>>(() => rBranches),
				new Lazy<IReadOnlyKeyedList<string, Commit>>(() => rCommits),
				new Lazy<Branch>(() => currentBranch),
				new Lazy<Commit>(() => currentCommit));

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
				branch.IsMultiBranch);
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
				commit.BranchId);
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
						parentBranch.ChildBranches.Add(xBranch);
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
					.TakeWhile(c => c.BranchName == subBranch.Name);

				if (commits.Any())
				{
					MCommit firstCommit = commits.Last();
					subBranch.FirstCommitId = firstCommit.Id;
					subBranch.ParentCommitId = firstCommit.FirstParentId;
				}
				else
				{
					if (LatestCommit.BranchName != null)
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
					string id = Guid.NewGuid().ToString();
					MSubBranch subBranch = groupByBranch.First();
					MBranch mBranch = new MBranch(subBranch.MRepository)
					{
						Id = id,
						Name = subBranch.Name,
						IsMultiBranch = subBranch.IsMultiBranch,
						IsActive = subBranch.IsActive,
						IsAnonymous = subBranch.IsAnonymous,
						ParentCommitId = subBranch.ParentCommitId
					};

					mBranch.SubBranches.AddRange(groupByBranch);
					mBranch.SubBranches.ForEach(b => b.BranchId = id);

					mBranch.Commits.AddRange(
						groupByBranch
							.SelectMany(branch =>
								new[] { branch.LatestCommit }
									.Where(c => c.SubBranchId == branch.Id && c.Id != branch.ParentCommitId) 
								.Concat(
									branch.LatestCommit
										.FirstAncestors()
										.TakeWhile(c => c.SubBranchId == branch.Id && c.Id != branch.ParentCommitId)))
								.Distinct()
							.OrderByDescending(c => c.CommitDate));

					if (mBranch.Commits.Any(c => c.BranchId != null))
					{
						Log.Error($"Commits belong to multiple branches {mBranch}");
					}

					mBranch.Commits.ForEach(c => c.BranchId = id);

					mBranch.LatestCommitId = mBranch.Commits.Any() 
						? mBranch.Commits.First().Id : mBranch.ParentCommitId;
					mBranch.FirstCommitId = mBranch.Commits.Any()
					? mBranch.Commits.Last().Id : mBranch.ParentCommitId;

					mBranch.MRepository.Branches.Add(mBranch);
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
				string branchName = xCommit.BranchName;
				string subBranchId = xCommit.SubBranchId;

				MCommit last = xCommit;
				bool isFound = false;
				foreach (MCommit current in xCommit.FirstAncestors())
				{
					if (current.HasBranchName && current.BranchName != branchName)
					{
						// found commit with branch name already set 
						break;
					}

					string currentBranchName = GetBranchName(current);
					if (string.IsNullOrEmpty(currentBranchName) || currentBranchName == branchName)
					{
						last = current;
					}

					if (currentBranchName == branchName)
					{
						isFound = true;
					}
				}

				if (isFound)
				{
					foreach (MCommit current in xCommit.FirstAncestors())
					{
						current.BranchName = branchName;
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
				string branchName = xCommit.BranchName;
				string subBranchId = xCommit.SubBranchId;

				foreach (MCommit current in xCommit.FirstAncestors()
					.TakeWhile(c => c.FirstChildIds.Count <= 1 && !c.HasBranchName))
				{
					current.BranchName = branchName;
					current.SubBranchId = subBranchId;

				}
			}
		}


		private IReadOnlyList<MSubBranch> AddMultiBranches(
			IReadOnlyList<MCommit> commits, IReadOnlyList<MSubBranch> branches, MRepository xmodel)
		{
			IEnumerable<MCommit> roots =
				commits.Where(c =>
				string.IsNullOrEmpty(c.BranchName)
				&& c.FirstChildIds.Count > 1);

			// The commits where multiple branches are starting and the commits has no branch name
			IEnumerable<MCommit> roots2 = branches
				.GroupBy(b => b.LatestCommitId)
				.Where(group => group.Count() > 1)
				.Select(group => xmodel.Commits[group.Key])
				.Where(c => string.IsNullOrEmpty(c.BranchName));

			roots = roots.Concat(roots2);

			List<MSubBranch> multiBranches = new List<MSubBranch>();
			foreach (MCommit root in roots)
			{
				string branchName = "Multibranch_" + root.ShortId;

				MSubBranch subBranch = new MSubBranch(xmodel)
				{
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
				&& !mRepository.SubBranches.Any(b =>b.LatestCommitId == commit.Id));

			IEnumerable<MCommit> pullMergeTopCommits = commits
				.Where(commit =>
					commit.HasSecondParent
					&& commit.MergeSourceBranchNameFromSubject != null
					&& commit.MergeSourceBranchNameFromSubject == commit.MergeTargetBranchNameFromSubject)
				.Select(c => c.SecondParent);

			topCommits = topCommits.Concat(pullMergeTopCommits).Distinct();


			foreach (MCommit xCommit in topCommits)
			{
				MSubBranch subBranch = new MSubBranch(mRepository)
				{
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
					&& ( b.LatestCommitId == id))
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

				if (!string.IsNullOrEmpty(mCommit.BranchName))
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

				mCommit.BranchName = subBranch.Name;
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


		private string GetBranchName(MCommit mCommit)
		{
			if (!string.IsNullOrEmpty(mCommit.BranchName))
			{
				return mCommit.BranchName;
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

				if (mCommit.BranchName == subBranch.Name)
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

				mCommit.BranchName = subBranch.Name;
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
					mCommit.BranchName = commitBranch.BranchName;
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
			
			return new MSubBranch(mRepository)
			{
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

			return new MCommit(mRepository)
			{
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