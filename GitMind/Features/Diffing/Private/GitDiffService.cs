//using System;
//using System.IO;
//using System.Threading.Tasks;
//using GitMind.ApplicationHandling;
//using GitMind.Common;
//using GitMind.Common.ProgressHandling;
//using GitMind.Features.StatusHandling;
//using GitMind.Git;
//using GitMind.Git.Private;
//using GitMind.Utils;


//namespace GitMind.Features.Diffing.Private
//{
//	internal class GitDiffService : IGitDiffService
//	{
//		private readonly WorkingFolder workingFolder;
//		private readonly IRepoCaller repoCaller;
//		private readonly IGitDiffParser gitDiffParser;
//		private readonly Lazy<IProgressService> progressService;
//		private readonly Lazy<IStatusService> statusService;


//		public GitDiffService(
//			WorkingFolder workingFolder,
//			IRepoCaller repoCaller,
//			IGitDiffParser gitDiffParser,
//			Lazy<IProgressService> progressService,
//			Lazy<IStatusService> statusService)
//		{
//			this.workingFolder = workingFolder;
//			this.repoCaller = repoCaller;
//			this.gitDiffParser = gitDiffParser;
//			this.progressService = progressService;
//			this.statusService = statusService;
//		}


//		public Task<R<CommitDiff>> GetFileDiffAsync(CommitSha commitSha, string path)
//		{
//			Log.Debug($"Get diff for file {path} for commit {commitSha} ...");
//			return repoCaller.UseRepoAsync(async repo =>
//			{
//				string patch = repo.Diff.GetFilePatch(commitSha, path);

//				CommitDiff commitDiff = await gitDiffParser.ParseAsync(commitSha, patch, false);

//				if (commitSha == CommitSha.Uncommitted)
//				{
//					string filePath = Path.Combine(workingFolder, path);
//					if (File.Exists(filePath))
//					{
//						commitDiff = new CommitDiff(commitDiff.LeftPath, filePath);
//					}
//				}

//				return commitDiff;
//			});
//		}


//		public Task<R<CommitDiff>> GetCommitDiffAsync(CommitSha commitSha)
//		{
//			Log.Debug($"Get diff for commit {commitSha} ...");
//			return repoCaller.UseRepoAsync(async repo =>
//			{
//				string patch = repo.Diff.GetPatch(commitSha);

//				return await gitDiffParser.ParseAsync(commitSha, patch);
//			});
//		}


//		public Task<R<CommitDiff>> GetPreviewMergeDiffAsync(CommitSha commitSha1, CommitSha commitSha2)
//		{
//			Log.Debug($"Get diff for pre-merge {commitSha1}-{commitSha2} ...");
//			return repoCaller.UseRepoAsync(async repo =>
//			{
//				MergePatch patch = repo.Diff.GetPreMergePatch(commitSha1, commitSha2);

//				CommitDiff diff = await gitDiffParser.ParseAsync(null, patch.Patch);

//				if (patch.ConflictPatch == "")
//				{
//					return diff;
//				}

//				// There where conflicts
//				string leftTempPath = diff.LeftPath + ".1";
//				string rigthTempPath = diff.RightPath + ".1";

//				if (File.Exists(leftTempPath))
//				{
//					File.Delete(leftTempPath);
//				}
//				if (File.Exists(rigthTempPath))
//				{
//					File.Delete(rigthTempPath);
//				}

//				File.Move(diff.LeftPath, leftTempPath);
//				File.Move(diff.RightPath, rigthTempPath);

//				CommitDiff conflictDiff = await gitDiffParser.ParseAsync(null, patch.ConflictPatch, true, true);

//				string left = File.ReadAllText(conflictDiff.LeftPath);
//				string right = File.ReadAllText(conflictDiff.RightPath);

//				string divider =
//					"========================================================" +
//					"=================================================\n" +
//					"########################################################" +
//					"#################################################\n" +
//					"========================================================" +
//					"=================================================\n";
//				string conflictText1 = divider + "NOTE: There are conflicts !!!\n\nFiles with conflicts:\n";
//				string conflictText2 = divider;

//				File.WriteAllText(conflictDiff.LeftPath, conflictText1);
//				File.WriteAllText(conflictDiff.RightPath, conflictText1);
//				File.AppendAllText(conflictDiff.LeftPath, left);
//				File.AppendAllText(conflictDiff.RightPath, right);
//				File.AppendAllText(conflictDiff.LeftPath, conflictText2);
//				File.AppendAllText(conflictDiff.RightPath, conflictText2);
//				File.AppendAllText(conflictDiff.LeftPath, File.ReadAllText(leftTempPath));
//				File.AppendAllText(conflictDiff.RightPath, File.ReadAllText(rigthTempPath));

//				File.Delete(leftTempPath);
//				File.Delete(rigthTempPath);

//				return conflictDiff;
//			});
//		}


//		public Task<R<CommitDiff>> GetCommitDiffRangeAsync(CommitSha commitSha1, CommitSha commitSha2)
//		{
//			Log.Debug($"Get diff for commit range {commitSha1}-{commitSha2} ...");
//			return repoCaller.UseRepoAsync(async repo =>
//			{
//				string patch = repo.Diff.GetPatchRange(commitSha1, commitSha2);

//				return await gitDiffParser.ParseAsync(null, patch);
//			});
//		}


//		public void GetFile(string fileId, string filePath)
//		{
//			Log.Debug($"Get file {fileId}, {filePath} ...");
//			repoCaller.UseRepo(repo => repo.GetFile(fileId, filePath));
//		}


//		public Task ResolveAsync(string path)
//		{
//			Log.Debug($"Resolve {path}  ...");

//			using (statusService.Value.PauseStatusNotifications())
//			using (progressService.Value.ShowDialog("Resolving ..."))
//			{
//				return repoCaller.UseRepoAsync(repo => repo.Resolve(path));
//			}
//		}
//	}
//}