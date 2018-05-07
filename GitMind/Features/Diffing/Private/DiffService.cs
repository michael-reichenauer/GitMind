using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GitMind.ApplicationHandling;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Common;
using GitMind.Common.MessageDialogs;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind.Features.Diffing.Private
{
	internal class DiffService : IDiffService
	{
		private static readonly string Base = "BASE";
		private static readonly string Yours = "YOURS";
		private static readonly string Theirs = "THEIRS";
		private static readonly string ConflictMarker = "<<<<<<< HEAD";

		private readonly WorkingFolder workingFolder;
		private readonly IGitDiffService gitDiffService;
		private readonly IGitStatusService2 gitStatusService2;
		private readonly IGitDiffParser diffParser;
		private readonly ICmd cmd;


		public DiffService(
			WorkingFolder workingFolder,
			IGitDiffService gitDiffService,
			IGitStatusService2 gitStatusService2,
			IGitDiffParser diffParser,
			ICmd cmd)
		{
			this.workingFolder = workingFolder;
			this.gitDiffService = gitDiffService;
			this.gitStatusService2 = gitStatusService2;
			this.diffParser = diffParser;
			this.cmd = cmd;
		}


		public async Task ShowDiffAsync(CommitSha commitSha)
		{
			string patch;
			if (commitSha == CommitSha.Uncommitted)
			{
				if (!(await gitDiffService.GetUncommittedDiffAsync(
					CancellationToken.None)).HasValue(out patch))
				{
					return;
				}
			}
			else
			{
				if (!(await gitDiffService.GetCommitDiffAsync(
					commitSha.Sha, CancellationToken.None)).HasValue(out patch))
				{
					return;
				}
			}

			CommitDiff commitDiff = await diffParser.ParseAsync(commitSha, patch, true, false);
			await ShowDiffImplAsync(commitDiff.LeftPath, commitDiff.RightPath);
		}


		public async Task ShowFileDiffAsync(CommitSha commitSha, string path)
		{
			string patch;
			if (commitSha == CommitSha.Uncommitted)
			{
				if (!(await gitDiffService.GetUncommittedFileDiffAsync(
					path, CancellationToken.None)).HasValue(out patch))
				{
					return;
				}
			}
			else
			{
				if (!(await gitDiffService.GetFileDiffAsync(
					commitSha.Sha, path, CancellationToken.None)).HasValue(out patch))
				{
					return;
				}
			}

			CommitDiff commitDiff = await diffParser.ParseAsync(commitSha, patch, false, false);
			await ShowDiffImplAsync(commitDiff.LeftPath, commitDiff.RightPath);
		}


		public async Task ShowPreviewMergeDiffAsync(CommitSha commitSha1, CommitSha commitSha2)
		{

			if ((await gitDiffService.GetPreviewMergeDiffAsync(
				commitSha1.Sha, commitSha2.Sha, CancellationToken.None)).HasValue(out string patch))
			{
				CommitDiff commitDiff = await diffParser.ParseAsync(null, patch, true, false);
				await ShowDiffImplAsync(commitDiff.LeftPath, commitDiff.RightPath);
			}
		}


		public async Task ShowDiffRangeAsync(CommitSha commitSha1, CommitSha commitSha2)
		{
			await Task.Yield();
			if ((await gitDiffService.GetCommitDiffRangeAsync(
				commitSha1.Sha, commitSha2.Sha, CancellationToken.None)).HasValue(out string patch))
			{
				CommitDiff commitDiff = await diffParser.ParseAsync(null, patch, true, false);
				await ShowDiffImplAsync(commitDiff.LeftPath, commitDiff.RightPath);
			}
		}


		public async Task MergeConflictsAsync(CommitSha commitSha, CommitFile file)
		{
			CleanTempPaths(file);

			string basePath = GetPath(file, Base);
			string yoursPath = GetPath(file, Yours);
			string theirsPath = GetPath(file, Theirs);

			await GetFileAsync(file.Conflict.LocalId, yoursPath);
			await GetFileAsync(file.Conflict.RemoteId, theirsPath);
			await GetFileAsync(file.Conflict.BaseId, basePath);

			if (File.Exists(yoursPath) && File.Exists(theirsPath) && File.Exists(basePath))
			{
				await ShowMergeImplAsync(theirsPath, yoursPath, basePath, file.FullFilePath);

				if (!HasConflicts(file))
				{
					await ResolveAsync(file.Path, file.FullFilePath);
				}
			}

			CleanTempPaths(file);
		}


		private async Task GetFileAsync(string fileId, string path)
		{
			if (string.IsNullOrEmpty(fileId))
			{
				File.WriteAllText(path, "");
				return;
			}

			R<string> yoursFile = await gitStatusService2.GetConflictFile(fileId, CancellationToken.None);
			if (yoursFile.IsOk)
			{
				File.WriteAllText(path, yoursFile.Value);
			}
		}


		public bool CanMergeConflict(CommitFile file) =>
			file.Status.HasFlag(GitFileStatus.ConflictMM) ||
			file.Status.HasFlag(GitFileStatus.ConflictAA);


		public async Task UseYoursAsync(CommitFile file)
		{
			CleanTempPaths(file);

			await UseFileAsync(file, file.Conflict.LocalId);

			await ResolveAsync(file.Path, file.FullFilePath);
		}



		public bool CanUseYours(CommitFile file) => !file.Status.HasFlag(GitFileStatus.ConflictDM);


		public async Task UseTheirsAsync(CommitFile file)
		{
			CleanTempPaths(file);

			await UseFileAsync(file, file.Conflict.RemoteId);

			await ResolveAsync(file.Path, file.FullFilePath);
		}


		public bool CanUseTheirs(CommitFile file) => !file.Status.HasFlag(GitFileStatus.ConflictMD);


		public async Task UseBaseAsync(CommitFile file)
		{
			CleanTempPaths(file);
			await UseFileAsync(file, file.Conflict.BaseId);

			await ResolveAsync(file.Path, file.FullFilePath);
		}


		public bool CanUseBase(CommitFile file) => !file.Status.HasFlag(GitFileStatus.ConflictAA);


		public async Task DeleteAsync(CommitFile file)
		{
			await Task.Yield();
			CleanTempPaths(file);
			string fullPath = file.FullFilePath;

			DeletePath(fullPath);

			await ResolveAsync(file.Path, file.FullFilePath);
		}


		public bool CanDelete(CommitFile file)
		{
			return file.Status.HasFlag(GitFileStatus.Conflict);
		}


		public async Task ShowYourDiffAsync(CommitFile file)
		{
			string yoursPath = GetPath(file, Theirs);
			string basePath = GetPath(file, Base);

			await GetFileAsync(file.Conflict.LocalId, yoursPath);
			await GetFileAsync(file.Conflict.BaseId, basePath);

			if (File.Exists(yoursPath) && File.Exists(basePath))
			{
				await ShowDiffImplAsync(basePath, yoursPath);

				DeletePath(basePath);
				DeletePath(yoursPath);
			}
		}



		public async Task ShowTheirDiffAsync(CommitFile file)
		{
			string basePath = GetPath(file, Base);
			string theirsPath = GetPath(file, Theirs);

			await GetFileAsync(file.Conflict.BaseId, basePath);
			await GetFileAsync(file.Conflict.RemoteId, theirsPath);

			if (File.Exists(theirsPath) && File.Exists(basePath))
			{
				await ShowDiffImplAsync(basePath, theirsPath);

				DeletePath(basePath);
				DeletePath(theirsPath);
			}
		}

		public IReadOnlyList<string> GetAllTempNames()
		{
			List<string> names = new List<string>();

			names.Add(Base);
			names.Add(Yours);
			names.Add(Theirs);
			return names;
		}


		public void ShowDiff(CommitSha uncommittedId)
		{
			Task.Run(() => ShowDiffAsync(CommitSha.Uncommitted).Wait())
				.Wait();
		}


		private void CleanTempPaths(CommitFile file)
		{
			DeletePath(GetPath(file, Base));
			DeletePath(GetPath(file, Yours));
			DeletePath(GetPath(file, Theirs));
		}


		private async Task UseFileAsync(CommitFile file, string fileId)
		{
			await GetFileAsync(fileId, file.FullFilePath);
		}


		private async Task ShowDiffImplAsync(string theirs, string mine)
		{
			DiffTool diffTool = Settings.Get<Options>().DiffTool;
			if (!IsDiffSupported(diffTool))
			{
				return;
			}

			await Task.Run(() =>
			{
				string args = diffTool.Arguments
					.Replace("%theirs", $"\"{theirs}\"")
					.Replace("%mine", $"\"{mine}\"");

				cmd.Run(diffTool.Command, args);
			});
		}


		private async Task ShowMergeImplAsync(
			string theirs, string mine, string basePath, string merged)
		{
			MergeTool mergeTool = Settings.Get<Options>().MergeTool;
			if (!IsMergeSupported(mergeTool))
			{
				return;
			}

			await Task.Run(() =>
			{
				string args = mergeTool.Arguments
					.Replace("%theirs", $"\"{theirs}\"")
					.Replace("%mine", $"\"{mine}\"")
					.Replace("%base", $"\"{basePath}\"")
					.Replace("%merged", $"\"{merged}\"");

				cmd.Run(mergeTool.Command, args);
			});
		}



		private static bool IsDiffSupported(DiffTool tool)
		{
			if (!File.Exists(tool.Command))
			{
				Message.ShowWarning(
					Application.Current.MainWindow,
					$"Could not locate diff tool:\n{tool.Command}.\n\nPlease edit DiffTool in the options.");
				return false;
			}

			if (!tool.Arguments.Contains("%theirs")
				|| !tool.Arguments.Contains("%mine"))
			{
				Message.ShowWarning(
					Application.Current.MainWindow,
					"DiffTool arguments must contain '%theirs' and '%mine'");
				return false;
			}

			return true;
		}

		private static bool IsMergeSupported(MergeTool tool)
		{
			if (!File.Exists(tool.Command))
			{
				Message.ShowWarning(
					Application.Current.MainWindow,
					$"Could not locate merge tool.\n{tool.Command}.\n\nPlease edit MergeTool in the options.");
				return false;
			}

			if (
				!tool.Arguments.Contains("%theirs")
				|| !tool.Arguments.Contains("%mine")
				|| !tool.Arguments.Contains("%base")
				|| !tool.Arguments.Contains("%merged"))
			{
				Message.ShowWarning(
					Application.Current.MainWindow,
					"MergeTool arguments must contain '%theirs', '%mine', '%base' and '%merged'");
				return false;
			}

			return true;
		}


		private string GetPath(CommitFile file, string type)
		{
			return GetConflictTypePath(file.FullFilePath, type);
		}


		private string GetConflictTypePath(string fullPath, string type)
		{
			string extension = Path.GetExtension(fullPath);
			return Path.ChangeExtension(fullPath, type + extension);
		}

		private bool HasConflicts(CommitFile file)
		{
			string fullPath = Path.Combine(workingFolder, file.Path);

			return
				File.Exists(fullPath)
				&& File.ReadAllLines(fullPath).Any(line => line.StartsWith(ConflictMarker));
		}


		private static void DeletePath(string path)
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}

		public async Task ResolveAsync(string path, string fullPath)
		{
			if (File.Exists(fullPath))
			{
				await gitStatusService2.AddAsync(path, CancellationToken.None);
			}
			else
			{
				await gitStatusService2.RemoveAsync(path, CancellationToken.None);
			}

			// Temp workaround to trigger status update after resolving conflicts, ill be handled better
			string tempPath = fullPath + ".resolve_tmp";
			File.AppendAllText(tempPath, "tmp");
			File.Delete(tempPath);
		}

	}
}