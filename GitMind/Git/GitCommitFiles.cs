using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;


namespace GitMind.Git
{
	public class GitCommitFiles
	{
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
					.Added.Select(t => new GitFile(t.Path, null, false, true, false, false))
					.Concat(treeChanges.Deleted.Select(t => new GitFile(t.Path, null, false, false, true, false)))
					.Concat(treeChanges.Modified.Select(t => new GitFile(t.Path, null, true, false, false, false)))
					.Concat(treeChanges.Renamed.Select(t => new GitFile(t.Path, t.OldPath, false, false, false, true)))
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
					.Added.Select(t => new GitFile(t.FilePath, null, false, true, false, false))
					.Concat(GetUntracked(status, conflicts))
					.Concat(status.Removed.Select(t => new GitFile(t.FilePath, null, false, false, true, false)))
					.Concat(status.Missing.Select(t => new GitFile(t.FilePath, null, false, false, true, false)))
					.Concat(status.Modified.Select(t => new GitFile(t.FilePath, null, true, false, false, false)))				
					.Concat(status.RenamedInWorkDir.Select(t => new GitFile(
						t.FilePath, t.IndexToWorkDirRenameDetails.OldFilePath, false, false, false, true)))
					.Concat(status.RenamedInIndex.Select(t => new GitFile(
						t.FilePath, t.HeadToIndexRenameDetails.OldFilePath, false, false, false, true)))
					.Concat(status.Staged.Select(t => new GitFile(
						t.FilePath, null, true, false, false, false)))
					.ToList();

				Files = GetUniqueFiles(files);
			}
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
						gitFile.IsModified | file.IsModified,
						gitFile.IsAdded || file.IsAdded,
						gitFile.IsDeleted || file.IsDeleted,
						gitFile.IsRenamed || file.IsRenamed));
				}
			}
			return uniqueFiles;
		}


		public GitCommitFiles(string commitId, ConflictCollection conflicts)
		{
			Id = commitId;

			Files = conflicts
				.Select(c => new GitFile(c.Ours.Path, c.Theirs.Path, true, false, false, false))
				.ToList();
		}


		public string Id { get; set; }
		public IReadOnlyList<GitFile> Files { get; set; }


		private static IReadOnlyList<GitFile> GetUntracked(RepositoryStatus status, ConflictCollection conflicts)
		{
			List<GitFile> untracked = new List<GitFile>();
			// When there are conflicts, tools create temp files like these, lets filter them. 
			IEnumerable<string> conflictFiles = conflicts.Select(c => c.Ours.Path + ".LOCAL.")
				.Concat(conflicts.Select(c => c.Ancestor.Path + ".BASE."))
				.Concat(conflicts.Select(c => c.Theirs.Path + ".REMOTE."))
				.ToList();


			foreach (StatusEntry statusEntry in status.Untracked)
			{
				if (!conflictFiles.Any(f => statusEntry.FilePath.StartsWith(f)))
				{
					untracked.Add(new GitFile(statusEntry.FilePath, null, false, true, false, false));
				}
			}

			return untracked;
		}
	}
}