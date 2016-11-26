using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Features.Diffing;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;
using LibGit2Sharp;


namespace GitMind.Features.StatusHandling.Private
{
	[SingleInstance]
	internal class GitStatusService : IGitStatusService
	{
		public IDiffService gitDiffService { get; set; }
		private static readonly StatusOptions StatusOptions =
			new StatusOptions { DetectRenamesInWorkDir = true, DetectRenamesInIndex = true };

		private readonly WorkingFolder workingFolder;
		private readonly IRepoCaller repoCaller;
		private readonly IReadOnlyList<string> tempNames;


		public GitStatusService(
			WorkingFolder workingFolder,
			IDiffService gitDiffService,
			IRepoCaller repoCaller)
		{
			this.workingFolder = workingFolder;
			this.repoCaller = repoCaller;
			this.gitDiffService = gitDiffService;

			tempNames = gitDiffService.GetAllTempNames();
		}


		public Task<R<IReadOnlyList<string>>> GetBrancheIdsAsync()
		{
			return repoCaller.UseLibRepoAsync(repo =>
			{
				return repo.Branches.Select(b =>
						b.CanonicalName +
						b.Tip.Id.Sha +
						b.IsCurrentRepositoryHead +
						b.TrackedBranch?.CanonicalName)
					.Concat(repo.Tags.Select(t => t.CanonicalName + t.Target.Id.Sha))
					.ToReadOnlyList();
			});
		}


		public Task<R<Status>> GetCurrentStatusAsync()
		{
			return repoCaller.UseLibRepoAsync(GetStatus);
		}


		public R<Status> GetCurrentStatus()
		{
			return repoCaller.UseRepo(repo => GetStatus(repo));
		}

		private Status GetStatus(IRepository repo)
		{
			RepositoryStatus status = repo.RetrieveStatus(StatusOptions);
			ConflictCollection conflicts = repo.Index.Conflicts;
			RepositoryInformation info = repo.Info;
			bool isFullyMerged = repo.Index.IsFullyMerged;
			bool isMerging = info.CurrentOperation == CurrentOperation.Merge;
			string mergeMessage = info.Message;

			IReadOnlyList<StatusFile> conflictFiles = GetConflictFiles(conflicts);
			IReadOnlyList<StatusFile> changedFiles = GetChangedFiles(status, conflictFiles);

			return new Status(
				changedFiles,
				conflictFiles,
				isMerging,
				isFullyMerged,
				mergeMessage);
		}


		private IReadOnlyList<StatusFile> GetConflictFiles(ConflictCollection conflicts)
		{
			IReadOnlyList<StatusFile> files = conflicts
				.Select(c => new StatusFile(workingFolder, GetConflictPath(c), null, ToConflict(c), GitFileStatus.Conflict))
				.ToList();

			return files;
		}


		private IReadOnlyList<StatusFile> GetChangedFiles(
			RepositoryStatus status,
			IReadOnlyList<StatusFile> conflictFiles)
		{
			List<StatusFile> files = status
				.Added.Select(t => new StatusFile(workingFolder, t.FilePath, null, null, GitFileStatus.Added))
				.Concat(GetUntracked(status))
				.Concat(status.Removed.Select(t => new StatusFile(workingFolder, t.FilePath, null, null, GitFileStatus.Deleted)))
				.Concat(status.Missing.Select(t => new StatusFile(workingFolder, t.FilePath, null, null, GitFileStatus.Deleted)))
				.Concat(status.Modified.Select(t => new StatusFile(workingFolder, t.FilePath, null, null, GitFileStatus.Modified)))
				.Concat(status.RenamedInWorkDir.Select(t => new StatusFile(
					workingFolder, t.FilePath, t.IndexToWorkDirRenameDetails.OldFilePath, null, GitFileStatus.Renamed)))
				.Concat(status.RenamedInIndex.Select(t => new StatusFile(
					workingFolder, t.FilePath, t.HeadToIndexRenameDetails.OldFilePath, null, GitFileStatus.Renamed)))
				.Concat(status.Staged.Select(t => new StatusFile(
					workingFolder, t.FilePath, null, null, GitFileStatus.Modified)))
				.Concat(conflictFiles)
				.ToList();

			return files;
		}


		private IReadOnlyList<StatusFile> GetUntracked(RepositoryStatus status)
		{
			List<StatusFile> untracked = new List<StatusFile>();

			// When there are conflicts, tools create temp files like these, lets filter them. 		
			foreach (StatusEntry statusEntry in status.Untracked)
			{
				string filePath = statusEntry.FilePath;

				if (!IsTempFile(filePath))
				{
					untracked.Add(new StatusFile(workingFolder, filePath, null, null, GitFileStatus.Added));
				}
			}

			return untracked;
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

		private bool IsTempFile(string filePath)
		{
			return tempNames.Any(name => -1 != filePath.IndexOf($".{name}.", StringComparison.Ordinal));
		}

		private static string GetConflictPath(Conflict conflict)
		{
			return conflict.Ours?.Path ?? conflict.Ancestor?.Path ?? conflict.Theirs?.Path;
		}
	}
}