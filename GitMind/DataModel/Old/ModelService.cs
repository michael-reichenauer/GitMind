using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.DataModel.Private;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;


namespace GitMind.DataModel.Old
{
	internal class ModelService : IModelService
	{
		private static readonly int VirtualMergeLimit = 10;

		private static readonly CommitComparer CommitComparer = new CommitComparer();

		private readonly IGitService gitService;
		private readonly IGitCacheService gitCacheService;
		private IXModelService xModelService = new XModelService();

		public ModelService()
			: this(new GitService(), new GitCacheService())
		{
		}


		public ModelService(
			IGitService gitService, 
			IGitCacheService gitCacheService)
		{
			this.gitService = gitService;
			this.gitCacheService = gitCacheService;
		}


		public async Task<Model> GetCachedModelAsync(IReadOnlyList<string> activeBranchNames)
		{
			R<IGitRepo> gitRepo = await gitCacheService.GetRepoAsync(null);

			if (gitRepo.IsFaulted)
			{
				// No cached repo available get fresh repo
				gitRepo = await gitService.GetRepoAsync(null);

				if (gitRepo.HasValue)
				{
					gitCacheService.UpdateAsync(null, gitRepo.Value).RunInBackground();
				}
			}

			List<ActiveBranch> activeBranches = activeBranchNames
				.Select(name => new ActiveBranch(name, null)).ToList();

			return await GetModelAsync(activeBranches, gitRepo.Value);
		}


		public async Task<Model> RefreshAsync(Model model)
		{
			R<IGitRepo> gitRepo = await gitService.GetRepoAsync(null);
			if (!gitRepo.HasValue)
			{
				Log.Warn($"Failed to refresh status [{gitRepo.Error}");
				return model;
			}

			gitCacheService.UpdateAsync(null, gitRepo.Value).RunInBackground();

			List<ActiveBranch> activeBranches = model.Branches
				.Where(b => !b.IsMultiBranch)
				.Select(b => new ActiveBranch(b.Name, b.LatestCommit.Id))
				.ToList();

			return await GetModelAsync(activeBranches, gitRepo.Value);
		}


		public async Task<Model> WithRemoveBranchNameAsync(Model model, string branchName)
		{
			List<BranchBuilder> branches = model.Branches.Select(b => (BranchBuilder)b).ToList();

			BranchBuilder branch = branches.First(b => b.Name == branchName);

			IReadOnlyList<BranchBuilder> ancestorBranches = GetAncestorBranches(branch, branches);

			List<ActiveBranch> activeBranches = new List<ActiveBranch>();

			foreach (BranchBuilder branchBuilder in branches)
			{
				if (!branchBuilder.IsMultiBranch
					&& branchBuilder != branch
					&& !ancestorBranches.Contains(branchBuilder))
				{
					activeBranches.Add(new ActiveBranch(branchBuilder.Name, branchBuilder.LatestCommit.Id));
				}
			}

			return await GetModelAsync(activeBranches, model.GitRepo);
		}


		public async Task<Model> WithToggleCommitAsync(Model model, Commit commit)
		{
			Commit secondParent = commit.SecondParent;

			if (model.Commits.Contains(secondParent))
			{
				// Close child branch.
				// Branches are sorted so find out if the commit or the secondParent is on the 
				// parent branch
				IBranch branch = model.Branches.First(
					b => b.Name == commit.Branch.Name || b.Name == secondParent.Branch.Name);

				if (branch.Name == commit.Branch.Name)
				{
					// The commit is on the parent branch, so close the branch of the secondParent commit
					return await WithRemoveBranchNameAsync(model, secondParent.Branch.Name);
				}
				else
				{
					// The commit was on the child branch, lets close the branch of the commit
					return await WithRemoveBranchNameAsync(model, commit.Branch.Name);
				}
			}

			// Open child branch.
			string branchName = secondParent.TryGetBranchNameFromSubject();

			if (branchName != null && model.GitRepo.TryGetBranch(branchName) != null)
			{
				return await WithAddBranchNameAsync(model, branchName, secondParent.Id);
			}

			return await WithAddBranchNameAsync(model, branchName, secondParent.Id);
		}


		public async Task<Model> WithAddBranchNameAsync(Model model, string branchName, string commitId)
		{
			List<ActiveBranch> activeBranches = model.Branches
				.Where(b => !b.IsMultiBranch)
				.Select(b => new ActiveBranch(b.Name, b.LatestCommit.Id))
				.ToList();

			if (!activeBranches.Any(b => b.Name == branchName))
			{
				activeBranches.Add(new ActiveBranch(branchName, commitId));
			}

			return await GetModelAsync(activeBranches, model.GitRepo);
		}





		private Task<Model> GetModelAsync(IReadOnlyList<ActiveBranch> activeBranches, IGitRepo gitRepo)
		{
			Timing timing = new Timing();

			return Task.Run(() =>
			{
				xModelService.XGetModel(gitRepo);

				if (!activeBranches.Any())
				{
					activeBranches = new[] { new ActiveBranch(gitRepo.GetCurrentBranch().Name, null) };
				}

				if (!activeBranches.Any(b => b.Name == "master"))
				{
					activeBranches = new[] { new ActiveBranch("master", null) }.Concat(activeBranches).ToList();
				}

				BranchPriority branchPriority = new BranchPriority();

				ModelBuilder model = new ModelBuilder(gitRepo, branchPriority);

				AddBranches(model, activeBranches);
				AddReferencedBranches(model);

				AddMultiBranches(model);
				ReduseMultiBranches(model);
				FixEmptyBranches(model);
				SortBranchcommits(model);
				SetMultiBranchBranches(model);

				MoveBranchCommitsToCorrectBranches(model);

				SetBranchesOnBranchCommits(model);

				SetBranchParents(model);

				SetActiveBranches(model, activeBranches);

				SetActiveBranchesOnBranchCommits(model);
				SetAheadBehindCommits(model);

				SetMerges(model);
				OptimizeVirtualMerges(model);
				SetTags(model);


				IReadOnlyList<BranchBuilder> activeBrancheBuilders = branchPriority.GetSortedBranches(
					model.ActiveBranches);

				List<Commit> commits = activeBrancheBuilders
					.SelectMany(branch => branch.Commits).ToList();
				commits.Sort(CommitComparer);

				Commit currentCommit = model.Commits.GetById(gitRepo.CurrentCommitId);

				IReadOnlyList<string> allBranchNames = GetAllBranchNames(gitRepo, branchPriority);

				Model model1 = new Model(
					activeBrancheBuilders,
					commits,
					id => model.Commits.GetById(id),
					model.Merges,
					currentCommit,
					gitRepo.GetCurrentBranch().Name,
					allBranchNames,
					gitRepo);


				// Log.Debug($"Done get model, time {Timing}");
				return model1;
			});
		}


		private void FixEmptyBranches(ModelBuilder model)
		{
			foreach (var branch in model.AllBranches.Where(b => b.Commits.Count == 0))
			{
				Commit parentCommit = branch.LatestCommit;

				Commit commit = new Commit(
					parentCommit.Id + "_1",
					new Lazy<IReadOnlyList<Commit>>(() => new[] { parentCommit }),
					new Lazy<IReadOnlyList<Commit>>(() => new Commit[0]),
					$"<<< no commits yet on branch: {branch.Name} >>>",
					"",
					parentCommit.DateTime,
					parentCommit.CommitDateTime + TimeSpan.FromMilliseconds(1),
					null);

				commit.Branches.Add(branch);
				branch.CommitsBuilder.Add(commit);
			}
		}


		private void MoveBranchCommitsToCorrectBranches(ModelBuilder model)
		{
			foreach (BranchBuilder branch in model.AllBranches.Where(b => !b.IsMultiBranch))
			{
				foreach (Commit commit in branch.Commits)
				{
					if (commit.BranchName != null && commit.BranchName != branch.Name)
					{
						Log.Debug($"Commit {commit} specified to be in {commit.BranchName} != {branch.Name}");
					}

					string branchName = commit.TryGetBranchNameFromSubject();
					if (branchName != null && branchName != branch.Name && commit.DateTime.Year > 2014)
					{
						//Log.Debug($"Commit {commit} subject branch is {branchName} != {branch.Name}");
					}
				}
			}
		}


		private static IReadOnlyList<BranchBuilder> GetAncestorBranches(
			BranchBuilder branch,
			IReadOnlyList<BranchBuilder> branches)
		{
			IEnumerable<BranchBuilder> childBranches = branches.Where(b => b.Parent == branch);

			List<BranchBuilder> ancestors = new List<BranchBuilder>();
			foreach (BranchBuilder childBranch in childBranches)
			{
				ancestors.Add(childBranch);
				ancestors.AddRange(GetAncestorBranches(childBranch, branches));
			}

			return ancestors;
		}

		private void SortBranchcommits(ModelBuilder model)
		{
			foreach (BranchBuilder branch in model.AllBranches)
			{
				branch.CommitsBuilder.Sort(CommitComparer);
			}
		}


		private void SetActiveBranches(ModelBuilder model, IReadOnlyList<ActiveBranch> activeBranches)
		{
			foreach (ActiveBranch activeBranch in activeBranches)
			{
				BranchBuilder branch = model.AllBranches.FirstOrDefault(b => b.Name == activeBranch.Name);
				if (branch == null)
				{
					branch = model.AllBranches.FirstOrDefault(b => b.LatestCommit.Id == activeBranch.CommitId);
				}

				if (branch != null)
				{
					do
					{
						if (!model.ActiveBranches.Any(b => b == branch))
						{
							model.ActiveBranches.Add(branch);
						}

						branch = branch.Parent;
					} while (branch != BranchBuilder.None);
				}
			}

			foreach (BranchBuilder branch in model.ActiveBranches.ToList())
			{
				// Is there some other named branch, which has a "real" name but same latest commit?
				BranchBuilder otherBranch = model.ActiveBranches.FirstOrDefault(b =>
				b.LatestCommit.Id == branch.Name && b.Name != b.LatestCommit.Id);

				if (otherBranch != null && otherBranch != branch)
				{
					model.ActiveBranches.Remove(branch);
				}
			}
		}


		private void AddBranches(ModelBuilder model, IReadOnlyList<ActiveBranch> branches)
		{
			foreach (ActiveBranch activeBranch in branches)
			{
				BranchBuilder branch = GetBranch(model, activeBranch);

				if (branch != BranchBuilder.None)
				{
					model.AllBranches.Add(branch);

					SetBranchCommits(model, branch);
				}
			}
		}


		private void AddReferencedBranches(ModelBuilder modelBuilder)
		{
			IReadOnlyList<BranchBuilder> branches = modelBuilder.AllBranches
				.Where(b => b.Name != "master")
				.ToList();

			foreach (BranchBuilder branch in branches)
			{
				IReadOnlyList<BranchBuilder> referencedBranches =
					GetReferencedBranches(modelBuilder, branch);

				foreach (BranchBuilder referencedBranch in referencedBranches)
				{
					if (!modelBuilder.AllBranches.Any(b => b.Name == referencedBranch.Name))
					{
						modelBuilder.AllBranches.Add(referencedBranch);

						SetBranchCommits(modelBuilder, referencedBranch);
					}
				}
			}
		}


		private void SetAheadBehindCommits(ModelBuilder model)
		{
			foreach (BranchBuilder branch in model.ActiveBranches)
			{
				Commit firstCommit = branch.FirstCommit;

				if (branch.LatestTrackingCommit != Commit.None
					&& branch.LatestTrackingCommit != branch.LatestLocalCommit)
				{
					MarkIsLocalAhead(branch.LatestLocalCommit, firstCommit, branch);
					MarkIsRemoteAhead(branch.LatestTrackingCommit, firstCommit, branch);
				}
				else
				{
					foreach (Commit commit in branch.Commits)
					{
						commit.IsLocalAheadMarker = true;
						commit.IsRemoteAheadMarker = true;
					}
				}

				int localAheadCount = 0;
				int remoteAheadCount = 0;
				foreach (Commit commit in branch.Commits)
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
		}


		private void MarkIsLocalAhead(Commit commit, Commit firstCommit, BranchBuilder branch)
		{
			if (!commit.IsLocalAheadMarker && commit.Branch == branch)
			{
				commit.IsLocalAheadMarker = true;
				if (commit != firstCommit)
				{
					foreach (Commit parent in commit.Parents)
					{
						MarkIsLocalAhead(parent, firstCommit, branch);
					}
				}
			}
		}


		private void MarkIsRemoteAhead(Commit commit, Commit firstCommit, BranchBuilder branch)
		{
			if (!commit.IsRemoteAheadMarker && commit.Branch == branch)
			{
				commit.IsRemoteAheadMarker = true;
				if (commit != firstCommit)
				{
					foreach (Commit parent in commit.Parents)
					{
						MarkIsRemoteAhead(parent, firstCommit, branch);
					}
				}
			}
		}


		private void SetActiveBranchesOnBranchCommits(ModelBuilder model)
		{
			foreach (BranchBuilder branch in model.ActiveBranches)
			{
				foreach (Commit commit in branch.Commits)
				{
					commit.ActiveBranch = branch;
				}
			}
		}

		private void SetBranchesOnBranchCommits(ModelBuilder model)
		{
			foreach (BranchBuilder branch in model.AllBranches)
			{
				foreach (Commit commit in branch.Commits)
				{
					Asserter.Requires(commit.Branch == null);
					commit.Branch = branch;
				}
			}
		}


		private static void SetBranchParents(ModelBuilder model)
		{
			foreach (BranchBuilder branch in model.AllBranches)
			{
				Commit firstCommit = branch.FirstCommit;
				Commit parentCommit = firstCommit.FirstParent;

				if (parentCommit != Commit.None)
				{
					Asserter.Requires(branch != parentCommit.Branch);
					branch.Parent = parentCommit.Branch;
				}
			}
		}


		private void ReduseMultiBranches(ModelBuilder modelBuilder)
		{
			IEnumerable<BranchBuilder> multiBranches = modelBuilder.AllBranches
				.Where(b => b.IsMultiBranch).ToList();

			foreach (BranchBuilder multiBranch in multiBranches)
			{
				List<Commit> commitsToMove = new List<Commit>();

				bool isCommitsMoved;
				do
				{
					commitsToMove.Clear();
					isCommitsMoved = false;

					if (!multiBranch.Commits.Any())
					{
						break;
					}

					IReadOnlyList<BranchBuilder> branches = multiBranch.Commits.First().Branches.ToList();
					Commit specifiedBranchNameCommit = multiBranch.Commits.FirstOrDefault(c => c.BranchName != null);
					foreach (Commit commit in multiBranch.Commits)
					{
						commitsToMove.Add(commit);
						if (specifiedBranchNameCommit != null && commit != specifiedBranchNameCommit)
						{
							continue;
						}

						BranchBuilder branch = null;
						if (commit == specifiedBranchNameCommit)
						{
							specifiedBranchNameCommit = null;
							branch = modelBuilder.AllBranches.FirstOrDefault(b => b.Name == commit.BranchName);
							if (branch == null)
							{
								branch = new BranchBuilder(
									commit.BranchName,
									null,
									multiBranch.Commits.First(),
									Commit.None);
								modelBuilder.AllBranches.Add(branch);
							}
						}
						else if (commit.Branches.Count == 1)
						{
							branch = commit.Branches.First();
						}
						else if (commit.Branches.Count == 3 && commit.Branches.Any(b => b.Name == commit.Id))
						{
							branch = commit.Branches.First(b => !b.IsMultiBranch && b.Name != commit.Id);
						}
						else
						{
							string branchName = commit.TryGetBranchNameFromSubject();
							if (branchName != null)
							{
								branch = modelBuilder.AllBranches.FirstOrDefault(b => b.Name == branchName);
							}
						}


						if (branch != null)
						{
							// Move commits from multi branch to branch
							branch.CommitsBuilder.AddRange(commitsToMove);
							foreach (Commit commitToMove in commitsToMove)
							{
								foreach (BranchBuilder builder in branches.Where(b => b != branch))
								{
									builder.CommitsBuilder.Remove(commitToMove);
								}

								commitToMove.Branches.Clear();
								commitToMove.Branches.Add(branch);
								multiBranch.CommitsBuilder.Remove(commitToMove);
							}

							// Reset the latest commit of the multi branch after the move of commits
							if (multiBranch.Commits.Any())
							{
								multiBranch.LatestLocalCommit = multiBranch.Commits.First();
							}
							else
							{
								modelBuilder.AllBranches.Remove(multiBranch);
							}

							// Remove other potential branches that "lost" from the rest of the 
							// multi branch commits
							foreach (BranchBuilder builder in branches.Where(b => b != branch))
							{
								foreach (Commit commit1 in multiBranch.Commits)
								{
									commit1.Branches.Remove(builder);
								}
							}

							// Moved some commits, lets recheck the commits in the multi commit and
							// search for more commits that can be moved
							isCommitsMoved = true;
							break;
						}
					}
				} while (isCommitsMoved);
			}
		}



		private void SetMultiBranchBranches(ModelBuilder modelBuilder)
		{
			IEnumerable<BranchBuilder> multiBranches = modelBuilder.AllBranches
				.Where(b => b.IsMultiBranch);

			foreach (BranchBuilder multiBranch in multiBranches)
			{
				foreach (Commit commit in multiBranch.Commits)
				{
					foreach (BranchBuilder commitbranch in commit.Branches)
					{
						if (!multiBranch.MultiBranches.Contains(commitbranch))
						{
							multiBranch.MultiBranches.Add(commitbranch);
						}
					}
				}
			}
		}


		private IReadOnlyList<BranchBuilder> GetReferencedBranches(
			ModelBuilder modelBuilder, BranchBuilder branch)
		{
			List<BranchBuilder> branches = new List<BranchBuilder>();

			Dictionary<string, Commit> checkedCommits = new Dictionary<string, Commit>();
			List<Commit> topCommits = new List<Commit>();

			foreach (Commit commit in branch.Commits)
			{
				CheckChildren(commit, checkedCommits, topCommits);
			}

			foreach (Commit commit in topCommits)
			{
				GitBranch gitBranch = modelBuilder.GitRepo.GetAllBranches()
					.FirstOrDefault(
						b => b.LatestCommitId == commit.Id
						|| b.LatestTrackingCommitId == commit.Id);

				if (gitBranch != null)
				{
					BranchBuilder branchBuilder = CreateBranch(modelBuilder, gitBranch);
					branches.Add(branchBuilder);
				}

				string branchName = commit.TryGetBranchNameFromSubject();
				if (gitBranch == null && branchName != null)
				{
					gitBranch = modelBuilder.GitRepo.TryGetBranch(branchName);

					if (gitBranch != null)
					{
						BranchBuilder branchBuilder = CreateBranch(modelBuilder, gitBranch);
						branches.Add(branchBuilder);
					}
					else
					{
						BranchBuilder branchBuilder = new BranchBuilder(
							branchName,
							null,
							commit,
							Commit.None);
						branches.Add(branchBuilder);
					}
				}
			}

			return branches;
		}


		private void CheckChildren(
			Commit commit,
			IDictionary<string, Commit> checkedCommits,
			ICollection<Commit> topCommits)
		{
			if (!checkedCommits.ContainsKey(commit.Id))
			{
				checkedCommits[commit.Id] = commit;

				if (!commit.Children.Any())
				{
					// The commit does not have any children, i.e. it is a top (latest) commit in a branch
					topCommits.Add(commit);
				}
				else if (commit.Children.All(child => child.FirstParent != commit))
				{
					// This commit has children, but no child has this commit has a first parent, i.e. this
					// commit is "top" of a branch. 
					topCommits.Add(commit);
				}
				else if (commit.Children.Count == 1 && commit.Children[0].SecondParent == commit)
				{
					// The commit has one child, which is merge, where this commit is the source, 
					// i.e. it is a top (latest) commit in a branch merged into some other branch.
					// However it could also be a "pull merge commit" lets treat is as a candidate for now
					topCommits.Add(commit);
				}
				foreach (Commit childCommit in commit.Children)
				{
					// Don't check children on master
					if (!childCommit.Branches.Any(b => b.Name == "master"))
					{
						CheckChildren(childCommit, checkedCommits, topCommits);
					}
				}

				//if (commit.SecondParent != Commit.None)
				//{
				//	CheckChildren(commit.SecondParent, checkedCommits, topCommits);
				//}
			}
		}


		private static BranchBuilder CreateBranch(ModelBuilder modelBuilder, GitBranch gitBranch)
		{
			string trackingBranchName = gitBranch.TrackingBranchName;
			Commit latestLocalCommit = modelBuilder.Commits.GetById(gitBranch.LatestCommitId);
			Commit latestTrackingCommit = gitBranch.LatestTrackingCommitId != null
				? modelBuilder.Commits.GetById(gitBranch.LatestTrackingCommitId)
				: Commit.None;

			BranchBuilder branch = new BranchBuilder(
				gitBranch.Name,
				trackingBranchName,
				latestLocalCommit,
				latestTrackingCommit);

			return branch;
		}


		private void SetBranchCommits(ModelBuilder modelBuilder, BranchBuilder branch)
		{
			Commit latestCommit = branch.LatestLocalCommit;

			IReadOnlyList<Commit> commits = GetPossibleBranchCommits(modelBuilder, branch, latestCommit);

			SetBranchCommits(commits, branch);

			SetPullMergeBranchCommits(commits, branch);

			if (branch.LatestTrackingCommit != Commit.None)
			{
				// The branch is a local branch, which has a remote tracking branch. The local branch can be
				// synced, ahead, behind or both ahead and behind the remote tracking branch 
				if (branch.LatestTrackingCommit == branch.LatestLocalCommit)
				{
					// Branch is synced, both local and remote latest are same
				}
				else if (branch.LatestTrackingCommit.Branches
					.FirstOrDefault(b => b.Name == branch.Name) != null)
				{
					// Remote tracking id commit already on this branch, i.e. local is ahead of remote.
				}
				else
				{
					// The remote tracking id commit is not yet set to this branch, i.e. local branch is 
					// behind the remote (or in conflict).
					// Setting all remote commits as part of branch
					var trackingCommits = GetPossibleBranchCommits(modelBuilder, branch, branch.LatestTrackingCommit);
					SetBranchCommits(trackingCommits, branch);
					SetPullMergeBranchCommits(trackingCommits, branch);
				}
			}
		}


		private void AddMultiBranches(ModelBuilder modelBuilder)
		{
			List<BranchBuilder> multiBranches = new List<BranchBuilder>();

			foreach (BranchBuilder branch in modelBuilder.AllBranches)
			{
				List<Commit> allBranchCommits = branch.Commits.ToList();
				branch.CommitsBuilder.Clear();

				BranchBuilder multiBranch = null;
				foreach (Commit commit in allBranchCommits)
				{
					Asserter.Requires(commit.Branches.Any());

					if (commit.Branches.Count == 1)
					{
						Asserter.Requires(commit.Branches.First() == branch);
						branch.CommitsBuilder.Add(commit);
					}
					else
					{
						// There are multiple branches for this commit 
						Asserter.Requires(!commit.Branches.Any(b => b.IsMultiBranch));

						if (multiBranch == null)
						{
							multiBranch = new BranchBuilder("Multi" + commit.Id, null, commit, Commit.None);
							multiBranch.IsMultiBranch = true;
							multiBranches.Add(multiBranch);
						}

						// Remove commit from other branches
						foreach (BranchBuilder builder in commit.Branches.ToList())
						{
							builder.CommitsBuilder.Remove(commit);
						}

						commit.Branches.Add(multiBranch);
						multiBranch.CommitsBuilder.Add(commit);
					}
				}

				branch.CommitsBuilder.Sort(CommitComparer);
			}

			foreach (BranchBuilder multiBranch in multiBranches)
			{
				Asserter.Requires(!modelBuilder.AllBranches.Any(b => b.Name == multiBranch.Name));
				if (!modelBuilder.AllBranches.Any(b => b.Name == multiBranch.Name))
				{
					modelBuilder.AllBranches.Add(multiBranch);
					multiBranch.CommitsBuilder.Sort(CommitComparer);
				}
			}
		}


		private IReadOnlyList<Commit> GetPossibleBranchCommits(
			ModelBuilder modelBuilder, BranchBuilder branch, Commit commit)
		{
			List<Commit> commits = new List<Commit>();

			while (commit != Commit.None)
			{
				if (CanBeBranchCommit(modelBuilder, branch, commit))
				{
					commits.Add(commit);

					if (!commit.Parents.Any())
					{
						break;
					}

					commit = commit.FirstParent;
				}
				else
				{
					break;
				}
			}

			return commits;
		}



		private void SetPullMergeBranchCommits(IReadOnlyList<Commit> commits, BranchBuilder branch)
		{
			foreach (Commit commit in commits)
			{
				if (commit.TryGetSourceBranchNameFromSubject() == branch.Name
					|| (commit.SecondParent != Commit.None
					&& commit.SecondParent.TryGetBranchNameFromSubject() == branch.Name))
				{
					IReadOnlyList<Commit> pullMergeCommits = GetPullMergeBranchCommits(
						commit.SecondParent, branch);
					SetBranchCommits(pullMergeCommits, branch);

					SetPullMergeBranchCommits(pullMergeCommits, branch);
				}
			}
		}


		private static IReadOnlyList<Commit> GetPullMergeBranchCommits(
			Commit commit, BranchBuilder branch)
		{
			List<Commit> commits = new List<Commit>();

			while (true)
			{
				if (commit.Branches.Any())
				{
					foreach (var otherBranch in commit.Branches.Where(b => b != branch))
					{
						// Log.Warn($"commit '{commit}' unexpectedly belongs to branch '{otherBranch}'");
					}

					break;
				}

				commits.Add(commit);

				if (commit.Parents.Count > 0)
				{
					commit = commit.FirstParent;
				}
				else
				{
					// Reached first commit of master
					break;
				}
			}

			return commits;
		}


		private void SetBranchCommits(IReadOnlyList<Commit> commits, BranchBuilder branch)
		{
			foreach (Commit commit in commits)
			{
				commit.Branches.Add(branch);
				branch.CommitsBuilder.Add(commit);
			}
		}

		private static void SetMerges(ModelBuilder model)
		{
			// Find merges for branches from parent branch to first commit on branch (no need for master)
			foreach (BranchBuilder branch in model.ActiveBranches
				.Where(b => b.Name != "master").ToList())
			{
				Commit childCommit = branch.FirstCommit;
				Commit parentCommit = childCommit.FirstParent;

				if (model.ActiveBranches.Contains(parentCommit.Branch))
				{
					// Creating a start of branch merge from parent branch to first commit on branch
					Merge merge = new Merge(parentCommit, childCommit, true, false);

					model.Merges.Add(merge);
				}
				else
				{
					Commit virtualParentCommit;
					if (TryFindVirtualParent(childCommit, out virtualParentCommit))
					{
						Merge merge = new Merge(virtualParentCommit, childCommit, true, true);

						model.Merges.Add(merge);
					}
				}
			}

			foreach (Commit commit in model.ActiveBranches.SelectMany(b => b.Commits))
			{
				if (commit.Parents.Count == 2)
				{
					Commit childCommit = commit;
					Commit parentCommit = childCommit.SecondParent;

					if (model.ActiveBranches.Contains(parentCommit.Branch))
					{
						if (parentCommit.Branch != childCommit.Branch)
						{
							Merge merge = new Merge(parentCommit, childCommit, false, false);
							childCommit.IsExpanded = true;
							model.Merges.Add(merge);
						}
					}
					else
					{
						Commit virtualParentCommit;
						if (TryFindVirtualParent(childCommit, out virtualParentCommit))
						{
							Merge merge = new Merge(virtualParentCommit, childCommit, false, true);

							model.Merges.Add(merge);
						}
					}
				}
			}
		}


		private static void OptimizeVirtualMerges(ModelBuilder modelBuilder)
		{
			List<Merge> merges = modelBuilder.Merges.Where(m => !m.IsVirtual).ToList();

			foreach (Merge merge in modelBuilder.Merges.Where(m => m.IsVirtual).Reverse())
			{
				if (merges.FirstOrDefault(m =>
					m.ParentCommit == merge.ParentCommit
					&& m.ChildCommit.Branch == merge.ChildCommit.Branch) == null)
				{
					merges.Add(merge);
				}
			}

			modelBuilder.Merges.Clear();
			modelBuilder.Merges.AddRange(merges);
		}



		private static bool CanBeBranchCommit(
			ModelBuilder modelBuilder, BranchBuilder branch, Commit commit)
		{
			// Checks if branch already exists in commitBranchNames or if some parent branch exists

			if (commit.BranchName != null && commit.BranchName != branch.Name)
			{
				// Log.Warn($"Commit {commit} should be on {commit.BranchName}, but is {branch.Name}");
			}

			return !commit.Branches.Any(b => b == branch || modelBuilder.IsParentBranch(b, branch));
		}


		private IReadOnlyList<string> GetAllBranchNames(IGitRepo gitRepo, BranchPriority branchPriority)
		{
			IReadOnlyList<GitBranch> allGitBranches = gitRepo.GetAllBranches();

			List<string> allBranchNames = allGitBranches.Select(b => b.Name).ToList();

			// Sort branch names so that the list follows the branch priority list and rules
			return branchPriority.GetSortedNames(allBranchNames);
		}


		public BranchBuilder GetBranch(ModelBuilder modelBuilder, ActiveBranch activeBranch)
		{
			GitBranch gitBranch = modelBuilder.GitRepo.TryGetBranch(activeBranch.Name);

			if (gitBranch == null && activeBranch.CommitId != null)
			{
				// The branch name does not exist in the git repository, it is probably an anonymous branch
				// where the name is the the id of the latest commit. Let try find that

				Commit commit = modelBuilder.Commits.GetById(activeBranch.CommitId);
				string branchName = commit.TryGetBranchNameFromSubject();

				if (branchName != null)
				{
					gitBranch = modelBuilder.GitRepo.TryGetBranch(branchName);
					if (gitBranch != null)
					{
						return CreateBranch(modelBuilder, gitBranch);
					}
				}
				//else
				{
					branchName = commit.Id;
				}

				string latestCommitId = commit.Id;

				gitBranch = new GitBranch(branchName, latestCommitId, false, null, null, false, true);
			}

			if (gitBranch != null)
			{
				if (activeBranch.CommitId != null)
				{
					GitCommit latestCommit = modelBuilder.GitRepo.GetCommit(gitBranch.LatestCommitId);
					GitCommit latestTrackingCommit = gitBranch.LatestTrackingCommitId != null
						? modelBuilder.GitRepo.GetCommit(gitBranch.LatestTrackingCommitId)
						: GitCommit.None;
					GitCommit activeCommit = modelBuilder.GitRepo.GetCommit(activeBranch.CommitId);
					if (activeCommit == latestCommit || activeCommit == latestTrackingCommit)
					{
						return CreateBranch(modelBuilder, gitBranch);
					}
					else if (activeCommit.CommitDate > latestCommit.CommitDate
						&& activeCommit.CommitDate > latestTrackingCommit.CommitDate)
					{
						return CreateBranch(modelBuilder, new GitBranch(
							gitBranch.Name,
							activeBranch.CommitId,
							gitBranch.IsCurrent,
							gitBranch.TrackingBranchName,
							gitBranch.LatestTrackingCommitId,
							gitBranch.IsRemote,
							gitBranch.IsAnonyous));

					}
					//else
					//{
					//	return CreateBranch(modelBuilder, gitBranch);
					//}
				}
			}

			if (gitBranch == null)
			{
				return BranchBuilder.None;
			}

			return CreateBranch(modelBuilder, gitBranch);
		}


		private static bool TryFindVirtualParent(Commit childCommit, out Commit parentCommit)
		{
			Commit parent;
			if (childCommit.Parents.Count == 1)
			{
				// The target is a first commit on a branch, while the source is first parent,
				// which is on some other parent branch, The merge is considered part of the main branch
				parent = childCommit.FirstParent;
			}
			else
			{
				// The target is a normal merge and source the second parent
				parent = childCommit.SecondParent;
			}

			return TryFindVirtualParentCommit(parent, childCommit.Branch, out parentCommit);
		}



		private static bool TryFindVirtualParentCommit(
			Commit rootNode, BranchBuilder childBranch, out Commit parentCommit)
		{
			for (int depth = 0; depth < VirtualMergeLimit; depth++)
			{
				if (TryFindVirtualParentCommit(rootNode, depth, childBranch, out parentCommit))
				{
					return true;
				}
			}

			// Did not find a parent on any depth
			parentCommit = null;
			return false;
		}


		private static bool TryFindVirtualParentCommit(
			Commit node, int depth, BranchBuilder childBranch, out Commit parentCommit)
		{
			if (depth == 0
				&& node.IsOnActiveBranch()
				&& node.Branch != childBranch)
			{
				// Found a parent on other active branch
				parentCommit = node;
				return true;
			}
			else if (depth > 0)
			{
				foreach (Commit parent in node.Parents)
				{
					if (TryFindVirtualParentCommit(parent, depth - 1, childBranch, out parentCommit))
					{
						return true;
					}
				}
			}

			// Did not find a parent on this depth
			parentCommit = null;
			return false;
		}


		private void SetTags(ModelBuilder modelBuilder)
		{
			foreach (Commit commit in modelBuilder.ActiveBranches.SelectMany(branch => branch.Commits))
			{
				IEnumerable<Tag> tags = modelBuilder.GitRepo
					.GetTags(commit.Id)
					.Select(tag => new Tag(tag.TagName));

				commit.Tags.AddRange(tags);
			}
		}
	}
}