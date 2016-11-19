using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GitMind.ApplicationHandling;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Common.MessageDialogs;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.Utils;


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
		private readonly ICmd cmd;


		public DiffService(
			WorkingFolder workingFolder,
			IGitDiffService gitDiffService,
			ICmd cmd)
		{
			this.workingFolder = workingFolder;
			this.gitDiffService = gitDiffService;
			this.cmd = cmd;
		}


		public async Task ShowDiffAsync(string commitId)
		{
			R<CommitDiff> commitDiff = await gitDiffService.GetCommitDiffAsync(commitId);

			if (commitDiff.HasValue)
			{
				await ShowDiffImplAsync(commitDiff.Value.LeftPath, commitDiff.Value.RightPath);
			}
		}


		public async Task ShowDiffRangeAsync(string id1, string id2)
		{
			R<CommitDiff> commitDiff = await gitDiffService.GetCommitDiffRangeAsync(id1, id2);

			if (commitDiff.HasValue)
			{
				await ShowDiffImplAsync(commitDiff.Value.LeftPath, commitDiff.Value.RightPath);
			}
		}


		public async Task MergeConflictsAsync(string id, CommitFile file)
		{
			CleanTempPaths(file);

			string basePath = GetPath(file, Base);
			string yoursPath = GetPath(file, Yours);
			string theirsPath = GetPath(file, Theirs);

			string fullPath = Path.Combine(workingFolder, file.Path);

			gitDiffService.GetFile(file.Conflict.OursId, yoursPath);
			gitDiffService.GetFile(file.Conflict.TheirsId, theirsPath);
			gitDiffService.GetFile(file.Conflict.BaseId, basePath);

			if (File.Exists(yoursPath) && File.Exists(theirsPath) && File.Exists(basePath))
			{
				await ShowMergeImplAsync(theirsPath, yoursPath, basePath, fullPath);

				if (!HasConflicts(file))
				{
					await gitDiffService.ResolveAsync(file.Path);
				}
			}

			CleanTempPaths(file);
		}



		public bool CanMergeConflict(CommitFile file)
		{
			return
				file.Status.HasFlag(GitFileStatus.Conflict)
				&& file.Conflict.BaseId != null
				&& file.Conflict.OursId != null
				&& file.Conflict.TheirsId != null;
		}


		public async Task UseYoursAsync(CommitFile file)
		{
			CleanTempPaths(file);

			UseFile(file, file.Conflict.OursId);

			await gitDiffService.ResolveAsync(file.Path);
		}



		public bool CanUseYours(CommitFile file)
		{
			return
				file.Status.HasFlag(GitFileStatus.Conflict)
				&& file.Conflict.OursId != null;
		}


		public async Task UseTheirsAsync(CommitFile file)
		{
			CleanTempPaths(file);

			UseFile(file, file.Conflict.TheirsId);

			await gitDiffService.ResolveAsync(file.Path);
		}


		public bool CanUseTheirs(CommitFile file)
		{
			return
				file.Status.HasFlag(GitFileStatus.Conflict)
				&& file.Conflict.TheirsId != null;
		}


		public async Task UseBaseAsync(CommitFile file)
		{
			CleanTempPaths(file);
			UseFile(file, file.Conflict.BaseId);

			await gitDiffService.ResolveAsync(file.Path);
		}


		public bool CanUseBase(CommitFile file)
		{
			return
				file.Status.HasFlag(GitFileStatus.Conflict)
				&& file.Conflict.BaseId != null;
		}


		public async Task DeleteAsync(CommitFile file)
		{
			CleanTempPaths(file);
			string fullPath = Path.Combine(workingFolder, file.Path);

			DeletePath(fullPath);

			await gitDiffService.ResolveAsync(file.Path);
		}


		public bool CanDelete(CommitFile file)
		{
			return file.Status.HasFlag(GitFileStatus.Conflict);
		}


		public async Task ShowYourDiffAsync(CommitFile file)
		{
			string yoursPath = GetPath(file, Theirs);
			string basePath = GetPath(file, Base);

			gitDiffService.GetFile(file.Conflict.OursId, yoursPath);
			gitDiffService.GetFile(file.Conflict.BaseId, basePath);

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

			gitDiffService.GetFile(file.Conflict.BaseId, basePath);
			gitDiffService.GetFile(file.Conflict.TheirsId, theirsPath);

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


		public void ShowDiff(string uncommittedId)
		{
			Task.Run(() => ShowDiffAsync(Commit.UncommittedId).Wait())
				.Wait();
		}


		private void CleanTempPaths(CommitFile file)
		{
			DeletePath(GetPath(file, Base));
			DeletePath(GetPath(file, Yours));
			DeletePath(GetPath(file, Theirs));
		}


		private void UseFile(CommitFile file, string fileId)
		{
			string fullPath = Path.Combine(workingFolder, file.Path);

			gitDiffService.GetFile(fileId, fullPath);
		}


		public async Task ShowFileDiffAsync(string commitId, string name)
		{
			R<CommitDiff> commitDiff = await gitDiffService.GetFileDiffAsync(commitId, name);

			if (commitDiff.HasValue)
			{
				await ShowDiffImplAsync(commitDiff.Value.LeftPath, commitDiff.Value.RightPath);
			}
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
			return GetPath(file.Path, type);
		}


		private string GetPath(string path, string type)
		{
			string fullPath = Path.Combine(workingFolder, path);
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
	}
}