//using System.Collections.Generic;
//using System.Linq;
//using GitMind.Common;
//using GitMind.Features.Diffing;
//using GitMind.Features.StatusHandling;
//using LibGit2Sharp;


//namespace GitMind.Git
//{
//	internal class GitDiff
//	{
//		private readonly Diff diff;
//		private readonly Repository repository;
//		private static readonly SimilarityOptions DetectRenames =
//			new SimilarityOptions { RenameDetectionMode = RenameDetectionMode.Renames };
//		private static readonly StatusOptions StatusOptions =
//			new StatusOptions { DetectRenamesInWorkDir = true, DetectRenamesInIndex = true };

//		private static readonly CompareOptions DefultCompareOptions = new CompareOptions
//		{ ContextLines = 5, Similarity = DetectRenames };
//		private static readonly CompareOptions DefultFileCompareOptions = new CompareOptions
//		{ ContextLines = 10000, Similarity = DetectRenames };


//		public GitDiff(Diff diff, Repository repository)
//		{
//			this.diff = diff;
//			this.repository = repository;
//		}


//		public string GetPatch(CommitSha commitSha)
//		{
//			if (commitSha == CommitSha.Uncommitted)
//			{
//				RepositoryStatus repositoryStatus = repository.RetrieveStatus(StatusOptions);

//				List<string> files = repositoryStatus
//					.Where(s => !s.State.HasFlag(FileStatus.Ignored))
//					.SelectMany(GetStatusFiles)
//					.ToList();

//				// Current working folder uncommitted changes
//				string compare = "";
//				if (files.Any())
//				{
//					if (repository.Head.Commits.Any())
//					{
//						compare = diff.Compare<Patch>(
//						repository.Head.Tip.Tree,
//						DiffTargets.WorkingDirectory,
//						files,
//						null,
//						DefultCompareOptions);
//					}
//					else
//					{
//						compare = diff.Compare<Patch>(null, true, null, DefultCompareOptions);
//					}
//				}

				

//				return compare;
//			}

//			Commit commit = repository.Lookup<Commit>(new ObjectId(commitSha.Sha));

//			if (commit != null)
//			{
//				Tree parentTree = null;
//				if (commit.Parents.Any())
//				{
//					parentTree = commit.Parents.First().Tree;
//				}

//				return diff.Compare<Patch>(
//					parentTree,
//					commit.Tree,
//					DefultCompareOptions);
//			}

//			return "";
//		}


//		private IEnumerable<string> GetStatusFiles(StatusEntry statusEntry)
//		{
//			if (statusEntry.State.HasFlag(FileStatus.RenamedInIndex))
//			{
//				return new[]
//				{
//					statusEntry.HeadToIndexRenameDetails.OldFilePath,
//					statusEntry.HeadToIndexRenameDetails.NewFilePath
//				};
//			}
//			else if (statusEntry.State.HasFlag(FileStatus.RenamedInWorkdir))
//			{
//				return new[]
//				{
//					statusEntry.IndexToWorkDirRenameDetails.OldFilePath,
//					statusEntry.IndexToWorkDirRenameDetails.NewFilePath
//				};
//			}
//			else
//			{
//				return new[] { statusEntry.FilePath };
//			}
//		}


//		public string GetPatchRange(CommitSha id1, CommitSha id2)
//		{
//			Commit commit1 = repository.Lookup<Commit>(new ObjectId(id1.Sha));
//			Commit commit2 = repository.Lookup<Commit>(new ObjectId(id2.Sha));

//			if (commit1 != null && commit2 != null)
//			{
//				return diff.Compare<Patch>(
//					commit2.Tree,
//					commit1.Tree,
//					DefultCompareOptions);
//			}

//			return "";
//		}


//		internal string GetFilePatch(CommitSha commitSha, string filePath)
//		{
//			if (commitSha == CommitSha.Uncommitted)
//			{
//				if (repository.Head.Commits.Any())
//				{
//					return diff.Compare<Patch>(
//						repository.Head.Tip.Tree,
//						DiffTargets.WorkingDirectory | DiffTargets.WorkingDirectory,
//						new[] {filePath},
//						null,
//						DefultFileCompareOptions);
//				}
//				else
//				{
//					// Current working folder uncommitted changes
//					return diff.Compare<Patch>(new[] { filePath }, true, null, DefultFileCompareOptions);
//				}
//			}

//			Commit commit = repository.Lookup<Commit>(new ObjectId(commitSha.Sha));

//			if (commit != null)
//			{
//				Tree parentTree = null;
//				if (commit.Parents.Any())
//				{
//					parentTree = commit.Parents.First().Tree;
//				}

//				return diff.Compare<Patch>(
//					parentTree,
//					commit.Tree,
//					new[] { filePath },
//					null,
//					DefultFileCompareOptions);
//			}

//			return "";
//		}


//		public IReadOnlyList<StatusFile> GetFiles(string workingFolder, CommitSha commitSha)
//		{
//			Commit commit = repository.Lookup<Commit>(new ObjectId(commitSha.Sha));

//			if (commit != null)
//			{
//				Tree parentTree = null;
//				if (commit.Parents.Any())
//				{
//					parentTree = commit.Parents.First().Tree;
//				}

//				TreeChanges treeChanges = diff.Compare<TreeChanges>(
//					parentTree,
//					commit.Tree,
//					DefultCompareOptions);

//				return GetChangedFiles(workingFolder, treeChanges);
//			}

//			return new List<StatusFile>();
//		}

//		private IReadOnlyList<StatusFile> GetChangedFiles(string workingFolder, TreeChanges treeChanges)
//		{
//			List<StatusFile> files = treeChanges
//					.Added.Select(t => new StatusFile(workingFolder, t.Path, null, null, GitFileStatus.Added))
//					.Concat(treeChanges.Deleted.Select(t => new StatusFile(workingFolder, t.Path, null, null, GitFileStatus.Deleted)))
//					.Concat(treeChanges.Modified.Select(t => new StatusFile(workingFolder, t.Path, null, null, GitFileStatus.Modified)))
//					.Concat(treeChanges.Renamed.Select(t => new StatusFile(workingFolder, t.Path, t.OldPath, null, GitFileStatus.Renamed)))
//					.ToList();

//			return GetUniqueFiles(workingFolder, files);
//		}


//		private static List<StatusFile> GetUniqueFiles(string workingFolder, List<StatusFile> files)
//		{
//			List<StatusFile> uniqueFiles = new List<StatusFile>();

//			foreach (StatusFile gitFile in files)
//			{
//				StatusFile file = uniqueFiles.FirstOrDefault(f => f.FilePath == gitFile.FilePath);
//				if (file == null)
//				{
//					uniqueFiles.Add(gitFile);
//				}
//				else
//				{
//					uniqueFiles.Remove(file);
//					uniqueFiles.Add(new StatusFile(
//						workingFolder,
//						file.FilePath,
//						gitFile.OldFilePath ?? file.OldFilePath,
//						gitFile.Conflict ?? file.Conflict,
//						gitFile.Status | file.Status));
//				}
//			}

//			return uniqueFiles;
//		}


//		public MergePatch GetPreMergePatch(CommitSha commitSha1, CommitSha commitSha2)
//		{
//			Commit commit1 = repository.Lookup<Commit>(new ObjectId(commitSha1.Sha));
//			Commit commit2 = repository.Lookup<Commit>(new ObjectId(commitSha2.Sha));

//			MergeTreeOptions options = new MergeTreeOptions();

//			options.MergeFileFavor = MergeFileFavor.Normal;
//			MergeTreeResult result = repository.ObjectDatabase.MergeCommits(commit1, commit2, options);

//			if (result.Status == MergeTreeStatus.Succeeded)
//			{
//				string patchText = repository.Diff.Compare<Patch>(
//					commit1.Tree,
//					result.Tree,
//					DefultCompareOptions);

//				return new MergePatch(patchText, "");
//			}

//			// There where conflicts, lets get one patch without conflicted files and
//			// one patch of the conflicted files. We use two patches, one where all conflicts where
//			// using "Theirs" and one patch with all conflicts using "Ours" changes. We can then
//			// create one patch with no files with conflicts and one patch with only conflicted files.
//			// We Start getting a patch of all files, using "Theirs" for conflicts.
//			options.MergeFileFavor = MergeFileFavor.Theirs;
//			MergeTreeResult theirsResult = repository.ObjectDatabase.MergeCommits(commit1, commit2, options);

//			// We getting a patch of all files, using "Ours" for conflicts.
//			options.MergeFileFavor = MergeFileFavor.Ours;
//			MergeTreeResult oursResult = repository.ObjectDatabase.MergeCommits(commit1, commit2, options);

//			// We get a patch of all changed files (both with and without conflicts)
//			Patch allDiffPatch = repository.Diff.Compare<Patch>(
//				commit1.Tree,
//				theirsResult.Tree,
//				DefultCompareOptions);

//			// We get a patch of only files with conflicts (compaing "Theirs" and "Ours" changes only)
//			Patch conflictsPatch = repository.Diff.Compare<Patch>(
//				theirsResult.Tree,
//				oursResult.Tree,
//				DefultCompareOptions);
		
//			return new MergePatch(allDiffPatch, conflictsPatch);
//		}
//	}
//}