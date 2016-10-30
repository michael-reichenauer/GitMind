using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GitMind.Common.MessageDialogs;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.GitModel;
using GitMind.Utils;


namespace GitMind.RepositoryViews
{
	internal class DiffService : IDiffService
	{
		private static readonly string Base = "BASE";
		private static readonly string Yours = "YOURS";
		private static readonly string Theirs = "THEIRS";
		private static readonly string ConflictMarker = "<<<<<<< HEAD";

		private readonly IGitDiffService gitDiffService;
		private readonly ICmd cmd;


		public DiffService(IGitDiffService gitDiffService, ICmd cmd)
		{
			this.gitDiffService = gitDiffService;
			this.cmd = cmd;
		}


		public DiffService()
			: this(new GitDiffService(), new Cmd())
		{
		}



		public async Task ShowDiffAsync(string commitId, string workingFolder)
		{
			string p4mergeExe;
			if (!IsDiffSupported(out p4mergeExe))
			{
				return;
			}

			R<CommitDiff> commitDiff = await gitDiffService.GetCommitDiffAsync(workingFolder, commitId);

			if (commitDiff.HasValue)
			{
				await Task.Run(() =>
				{
					cmd.Run(p4mergeExe, $"\"{commitDiff.Value.LeftPath}\" \"{commitDiff.Value.RightPath}\"");
				});
			}
		}


		public async Task ShowDiffRangeAsync(string id1, string id2, string workingFolder)
		{
			string p4mergeExe;
			if (!IsDiffSupported(out p4mergeExe))
			{
				return;
			}

			R<CommitDiff> commitDiff = await gitDiffService.GetCommitDiffRangeAsync(workingFolder, id1, id2);

			if (commitDiff.HasValue)
			{
				await Task.Run(() =>
				{
					cmd.Run(p4mergeExe, $"\"{commitDiff.Value.LeftPath}\" \"{commitDiff.Value.RightPath}\"");
				});
			}
		}


		public async Task MergeConflictsAsync(string workingFolder, string id, CommitFile file)
		{
			string p4mergeExe;
			if (!IsDiffSupported(out p4mergeExe))
			{
				return;
			}

			CleanTempPaths(workingFolder, file);

			string basePath = GetPath(workingFolder, file, Base);
			string yoursPath = GetPath(workingFolder, file, Yours);
			string theirsPath = GetPath(workingFolder, file, Theirs);

			string fullPath = Path.Combine(workingFolder, file.Path);

			gitDiffService.GetFile(workingFolder, file.Conflict.OursId, yoursPath);
			gitDiffService.GetFile(workingFolder, file.Conflict.TheirsId, theirsPath);
			gitDiffService.GetFile(workingFolder, file.Conflict.BaseId, basePath);

			if (File.Exists(yoursPath) && File.Exists(theirsPath) && File.Exists(basePath))
			{
				await Task.Run(() =>
				{
					cmd.Run(p4mergeExe, $"\"{basePath}\" \"{theirsPath}\"  \"{yoursPath}\" \"{fullPath}\"");
				});

				if (!HasConflicts(workingFolder, file))
				{
					await gitDiffService.ResolveAsync(workingFolder, file.Path);
				}
			}

			CleanTempPaths(workingFolder, file);
		}



		public bool CanMergeConflict(CommitFile file)
		{
			return
				file.Status.HasFlag(GitFileStatus.Conflict)
				&& file.Conflict.BaseId != null
				&& file.Conflict.OursId != null
				&& file.Conflict.TheirsId != null;
		}


		public async Task UseYoursAsync(string workingFolder, CommitFile file)
		{
			CleanTempPaths(workingFolder, file);

			UseFile(workingFolder, file, file.Conflict.OursId);

			await gitDiffService.ResolveAsync(workingFolder, file.Path);
		}



		public bool CanUseYours(CommitFile file)
		{
			return
				file.Status.HasFlag(GitFileStatus.Conflict)
				&& file.Conflict.OursId != null;
		}


		public async Task UseTheirsAsync(string workingFolder, CommitFile file)
		{
			CleanTempPaths(workingFolder, file);

			UseFile(workingFolder, file, file.Conflict.TheirsId);

			await gitDiffService.ResolveAsync(workingFolder, file.Path);
		}


		public bool CanUseTheirs(CommitFile file)
		{
			return
				file.Status.HasFlag(GitFileStatus.Conflict)
				&& file.Conflict.TheirsId != null;
		}


		public async Task UseBaseAsync(string workingFolder, CommitFile file)
		{
			CleanTempPaths(workingFolder, file);
			UseFile(workingFolder, file, file.Conflict.BaseId);

			await gitDiffService.ResolveAsync(workingFolder, file.Path);
		}


		public bool CanUseBase(string workingFolder, CommitFile file)
		{
			return
				file.Status.HasFlag(GitFileStatus.Conflict)
				&& file.Conflict.BaseId != null;
		}


		public async Task DeleteAsync(string workingFolder, CommitFile file)
		{
			CleanTempPaths(workingFolder, file);
			string fullPath = Path.Combine(workingFolder, file.Path);

			DeletePath(fullPath);

			await gitDiffService.ResolveAsync(workingFolder, file.Path);
		}


		public bool CanDelete(string workingFolder, CommitFile file)
		{
			return file.Status.HasFlag(GitFileStatus.Conflict);
		}


		public async Task ShowYourDiffAsync(string workingFolder, CommitFile file)
		{
			string p4mergeExe;
			if (!IsDiffSupported(out p4mergeExe))
			{
				return;
			}

			string yoursPath = GetPath(workingFolder, file, Theirs);
			string basePath = GetPath(workingFolder, file, Base);

			gitDiffService.GetFile(workingFolder, file.Conflict.OursId, yoursPath);
			gitDiffService.GetFile(workingFolder, file.Conflict.BaseId, basePath);

			if (File.Exists(yoursPath) && File.Exists(basePath))
			{
				await Task.Run(() =>
				{
					cmd.Run(p4mergeExe, $"\"{basePath}\" \"{yoursPath}\"");
				});

				DeletePath(basePath);
				DeletePath(yoursPath);
			}
		}



		public async Task ShowTheirDiffAsync(string workingFolder, CommitFile file)
		{
			string p4mergeExe;
			if (!IsDiffSupported(out p4mergeExe))
			{
				return;
			}

			string basePath = GetPath(workingFolder, file, Base);
			string theirsPath = GetPath(workingFolder, file, Theirs);

			gitDiffService.GetFile(workingFolder, file.Conflict.BaseId, basePath);
			gitDiffService.GetFile(workingFolder, file.Conflict.TheirsId, theirsPath);

			if (File.Exists(theirsPath) && File.Exists(basePath))
			{
				await Task.Run(() =>
				{
					cmd.Run(p4mergeExe, $"\"{basePath}\" \"{theirsPath}\"");
				});

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


		public void ShowDiff(string uncommittedId, string workingFolder)
		{
			Task.Run(() => ShowDiffAsync(Commit.UncommittedId, workingFolder).Wait())
				.Wait();
		}


		private void CleanTempPaths(string workingFolder, CommitFile file)
		{
			DeletePath(GetPath(workingFolder, file, Base));
			DeletePath(GetPath(workingFolder, file, Yours));
			DeletePath(GetPath(workingFolder, file, Theirs));
		}


		private void UseFile(string workingFolder, CommitFile file, string fileId)
		{
			string fullPath = Path.Combine(workingFolder, file.Path);

			gitDiffService.GetFile(workingFolder, fileId, fullPath);
		}


		public async Task ShowFileDiffAsync(string workingFolder, string commitId, string name)
		{
			string p4mergeExe;
			if (!IsDiffSupported(out p4mergeExe))
			{
				return;
			}

			R<CommitDiff> commitDiff = await gitDiffService.GetFileDiffAsync(workingFolder, commitId, name);

			if (commitDiff.HasValue)
			{
				await Task.Run(() =>
				{
					cmd.Run(p4mergeExe, $"\"{commitDiff.Value.LeftPath}\" \"{commitDiff.Value.RightPath}\"");
				});
			}
		}


		private static bool IsDiffSupported(out string p4mergeExe)
		{
			p4mergeExe = "C:\\Program Files\\Perforce\\p4merge.exe";

			if (!File.Exists(p4mergeExe))
			{
				Message.ShowWarning(
					Application.Current.MainWindow,
					"Could not locate compatible diff tool.\nPlease install Perforce p4merge.");
				return false;
			}

			return true;
		}


		private static string GetPath(string workingFolder, CommitFile file, string type)
		{
			return GetPath(workingFolder, file.Path, type);
		}


		private static string GetPath(string workingFolder, string path, string type)
		{
			string fullPath = Path.Combine(workingFolder, path);
			string extension = Path.GetExtension(fullPath);
			return Path.ChangeExtension(fullPath, type + extension);
		}

		private static bool HasConflicts(string workingFolder, CommitFile file)
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