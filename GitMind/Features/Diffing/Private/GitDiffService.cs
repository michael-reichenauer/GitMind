using System;
using System.IO;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Common;
using GitMind.Common.ProgressHandling;
using GitMind.Features.StatusHandling;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;


namespace GitMind.Features.Diffing.Private
{
	internal class GitDiffService : IGitDiffService
	{
		private readonly WorkingFolder workingFolder;
		private readonly IRepoCaller repoCaller;
		private readonly IGitDiffParser gitDiffParser;
		private readonly Lazy<IProgressService> progressService;
		private readonly Lazy<IStatusService> statusService;


		public GitDiffService(
			WorkingFolder workingFolder,
			IRepoCaller repoCaller,
			IGitDiffParser gitDiffParser,
			Lazy<IProgressService> progressService,
			Lazy<IStatusService> statusService)
		{
			this.workingFolder = workingFolder;
			this.repoCaller = repoCaller;
			this.gitDiffParser = gitDiffParser;
			this.progressService = progressService;
			this.statusService = statusService;
		}


		public Task<R<CommitDiff>> GetFileDiffAsync(CommitSha commitSha, string path)
		{
			Log.Debug($"Get diff for file {path} for commit {commitSha} ...");
			return repoCaller.UseRepoAsync(async repo =>
			{
				string patch = repo.Diff.GetFilePatch(commitSha, path);

				CommitDiff commitDiff = await gitDiffParser.ParseAsync(commitSha, patch, false);

				if (commitSha == CommitSha.Uncommitted)
				{
					string filePath = Path.Combine(workingFolder, path);
					if (File.Exists(filePath))
					{
						commitDiff = new CommitDiff(commitDiff.LeftPath, filePath);
					}
				}

				return commitDiff;
			});
		}


		public Task<R<CommitDiff>> GetCommitDiffAsync(CommitSha commitSha)
		{
			Log.Debug($"Get diff for commit {commitSha} ...");
			return repoCaller.UseRepoAsync(async repo =>
			{
				string patch = repo.Diff.GetPatch(commitSha);

				return await gitDiffParser.ParseAsync(commitSha, patch);
			});
		}


		public Task<R<CommitDiff>> GetPreviewMergeDiffAsync(CommitSha commitSha1, CommitSha commitSha2)
		{
			Log.Debug($"Get diff for pre-merge {commitSha1}-{commitSha2} ...");
			return repoCaller.UseRepoAsync(async repo =>
			{
				MergePatch patch = repo.Diff.GetPreMergePatch(commitSha1, commitSha2);

				CommitDiff diff = await gitDiffParser.ParseAsync(null, patch.Patch);

				if (patch.ConflictPatch == "")
				{
					return diff;
				}

				// There where conflicts
				if (File.Exists(diff.LeftPath + ".1"))
				{
					File.Delete(diff.LeftPath + ".1");
				}
				if (File.Exists(diff.RightPath + ".1"))
				{
					File.Delete(diff.RightPath + ".1");
				}
				File.Move(diff.LeftPath, diff.LeftPath + ".1");
				File.Move(diff.RightPath, diff.RightPath + ".1");
				CommitDiff conflictDiff = await gitDiffParser.ParseAsync(null, patch.ConflictPatch);
				File.AppendAllText(conflictDiff.LeftPath, File.ReadAllText(diff.LeftPath + ".1"));
				File.AppendAllText(conflictDiff.RightPath, File.ReadAllText(diff.RightPath + ".1"));

				File.Delete(diff.LeftPath + ".1");
				File.Delete(diff.RightPath + ".1");

				return conflictDiff;
			});
		}


		public Task<R<CommitDiff>> GetCommitDiffRangeAsync(CommitSha commitSha1, CommitSha commitSha2)
		{
			Log.Debug($"Get diff for commit range {commitSha1}-{commitSha2} ...");
			return repoCaller.UseRepoAsync(async repo =>
			{
				string patch = repo.Diff.GetPatchRange(commitSha1, commitSha2);

				return await gitDiffParser.ParseAsync(null, patch);
			});
		}


		public void GetFile(string fileId, string filePath)
		{
			Log.Debug($"Get file {fileId}, {filePath} ...");
			repoCaller.UseRepo(repo => repo.GetFile(fileId, filePath));
		}


		public Task ResolveAsync(string path)
		{
			Log.Debug($"Resolve {path}  ...");

			using (statusService.Value.PauseStatusNotifications())
			using (progressService.Value.ShowDialog("Resolving ..."))
			{
				return repoCaller.UseRepoAsync(repo => repo.Resolve(path));
			}
		}
	}
}