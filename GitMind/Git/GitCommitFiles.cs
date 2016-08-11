using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.RepositoryViews;
using LibGit2Sharp;


namespace GitMind.Git
{
	public class GitCommitFiles
	{
		private readonly IDiffService diffService = new DiffService();

		public GitCommitFiles(string commitId, TreeChanges treeChanges)
		{
			Id = commitId;
			if (treeChanges == null)
			{
				Files = new GitFile[0];
			}
			else
			{
				List<GitFile> files = treeChanges
					.Added.Select(t => new GitFile(t.Path, null, null, GitFileStatus.Added))
					.Concat(treeChanges.Deleted.Select(t => new GitFile(t.Path, null, null, GitFileStatus.Deleted)))
					.Concat(treeChanges.Modified.Select(t => new GitFile(t.Path, null, null, GitFileStatus.Modified)))
					.Concat(treeChanges.Renamed.Select(t => new GitFile(t.Path, t.OldPath, null, GitFileStatus.Renamed)))
					.ToList();

				Files = GetUniqueFiles(files);
			}
		}

		public GitCommitFiles(string commitId, RepositoryStatus status, ConflictCollection conflicts)
		{
			Id = commitId;
			if (status == null)
			{
				Files = new GitFile[0];
			}
			else
			{
				List<GitFile> files = status
					.Added.Select(t => new GitFile(t.FilePath, null, null, GitFileStatus.Added))
					.Concat(GetUntracked(status, conflicts))
					.Concat(status.Removed.Select(t => new GitFile(t.FilePath, null, null, GitFileStatus.Deleted)))
					.Concat(status.Missing.Select(t => new GitFile(t.FilePath, null, null, GitFileStatus.Deleted)))
					.Concat(status.Modified.Select(t => new GitFile(t.FilePath, null, null, GitFileStatus.Modified)))				
					.Concat(status.RenamedInWorkDir.Select(t => new GitFile(
						t.FilePath, t.IndexToWorkDirRenameDetails.OldFilePath, null, GitFileStatus.Renamed)))
					.Concat(status.RenamedInIndex.Select(t => new GitFile(
						t.FilePath, t.HeadToIndexRenameDetails.OldFilePath, null, GitFileStatus.Renamed)))
					.Concat(status.Staged.Select(t => new GitFile(
						t.FilePath, null, null, GitFileStatus.Modified)))
					.Concat(conflicts.Select(t => new GitFile(GetConflictPath(t), null, ToConflict(t), GitFileStatus.Conflict)))
					.ToList();

				Files = GetUniqueFiles(files);
			}
		}


		private GitConflict ToConflict(Conflict conflict)
		{
			GitConflict gitConflict = new GitConflict(
				GetConflictPath(conflict),
				conflict.Ours?.Id.Sha,
				conflict.Theirs?.Id.Sha,
				conflict.Ancestor?.Id.Sha);
			return gitConflict;
		}


		private static string GetConflictPath(Conflict conflict)
		{
			return conflict.Ours?.Path ?? conflict.Ancestor?.Path ?? conflict.Theirs?.Path;
		}


		private static List<GitFile> GetUniqueFiles(List<GitFile> files)
		{
			List<GitFile> uniqueFiles = new List<GitFile>();

			foreach (GitFile gitFile in files)
			{
				GitFile file = uniqueFiles.FirstOrDefault(f => f.File == gitFile.File);
				if (file == null)
				{
					uniqueFiles.Add(gitFile);
				}
				else
				{
					uniqueFiles.Remove(file);
					uniqueFiles.Add(new GitFile(
						file.File,
						gitFile.OldFile ?? file.OldFile,
						gitFile.Conflict ?? file.Conflict,
						gitFile.Status | file.Status));
				}
			}

			return uniqueFiles;
		}


		public GitCommitFiles(string commitId, ConflictCollection conflicts)
		{
			Id = commitId;

			Files = conflicts
				.Select(c => new GitFile(GetConflictPath(c), null, ToConflict(c), GitFileStatus.Conflict))
				.ToList();
		}


		public string Id { get; set; }
		public IReadOnlyList<GitFile> Files { get; set; }


		private IReadOnlyList<GitFile> GetUntracked(RepositoryStatus status, ConflictCollection conflicts)
		{
			List<GitFile> untracked = new List<GitFile>();

			// When there are conflicts, tools create temp files like these, lets filter them. 
			IReadOnlyList<string> tempNames = diffService.GetAllTempNames();
			foreach (StatusEntry statusEntry in status.Untracked)
			{
				string filePath = statusEntry.FilePath;

				if (!tempNames.Any(name => -1 != filePath.IndexOf($".{name}.", StringComparison.Ordinal)))
				{
					untracked.Add(new GitFile(filePath, null, null, GitFileStatus.Added));
				}
			}

			return untracked;
		}
	}
}