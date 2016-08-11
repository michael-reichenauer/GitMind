using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.GitModel;
using GitMind.Settings;
using GitMind.Utils;


namespace GitMind.RepositoryViews
{
	internal class DiffService : IDiffService
	{
		private static readonly string Base = "BASE";
		private static readonly string Yours = "YOURS";
		private static readonly string Theirs = "THEIRS";
		//private static readonly string UseBase = "USEBASE";
		//private static readonly string UseYours = "USEYOURS";
		//private static readonly string UseTheirs = "USETHEIRS";
		//private static readonly string Deleted = "DELETED";
		//private static readonly string Merged = "MERGED";
		private static readonly string ConflictMarker = "<<<<<<< HEAD";

		private readonly IGitService gitService;
		private readonly ICmd cmd;


		public DiffService(IGitService gitService, ICmd cmd)
		{
			this.gitService = gitService;
			this.cmd = cmd;
		}


		public DiffService()
			: this(new GitService(), new Cmd())
		{
		}


		public async Task ShowDiffAsync(string commitId, string workingFolder)
		{
			string p4mergeExe;
			if (!IsDiffSupported(out p4mergeExe))
			{
				return;
			}

			R<CommitDiff> commitDiff = await gitService.GetCommitDiffAsync(workingFolder, commitId);

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

			R<CommitDiff> commitDiff = await gitService.GetCommitDiffRangeAsync(workingFolder, id1, id2);

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

			gitService.GetFile(workingFolder, file.Conflict.OursId, yoursPath);
			gitService.GetFile(workingFolder, file.Conflict.TheirsId, theirsPath);
			gitService.GetFile(workingFolder, file.Conflict.BaseId, basePath);

			if (File.Exists(yoursPath) && File.Exists(theirsPath) && File.Exists(basePath))
			{
				await Task.Run(() =>
				{
					cmd.Run(p4mergeExe, $"\"{basePath}\" \"{theirsPath}\"  \"{yoursPath}\" \"{fullPath}\"");			
				});

				if (!HasConflicts(workingFolder, file))
				{
					await gitService.ResolveAsync(workingFolder, file.Path);
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


		//public Task ResolveAsync(string workingFolder, CommitFile file)
		//{
		//	CleanTempPaths(workingFolder, file);
		//	return gitService.ResolveAsync(workingFolder, file.Path);
		//}


		//public bool CanResolve(string workingFolder, CommitFile file)
		//{
		//	return 
		//		IsMerged(workingFolder, file) 
		//		|| IsDeleted(workingFolder, file)
		//		|| IsUseBase(workingFolder, file)
		//		|| IsUseYours(workingFolder, file)
		//		|| IsUseTheirs(workingFolder, file);		
		//}



		public async Task UseYoursAsync(string workingFolder, CommitFile file)
		{
			CleanTempPaths(workingFolder, file);

			UseFile(workingFolder, file, file.Conflict.OursId);

			await gitService.ResolveAsync(workingFolder, file.Path);
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

			await gitService.ResolveAsync(workingFolder, file.Path);
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

			await gitService.ResolveAsync(workingFolder, file.Path);
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

			await gitService.ResolveAsync(workingFolder, file.Path);
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
		
			gitService.GetFile(workingFolder, file.Conflict.OursId, yoursPath);
			gitService.GetFile(workingFolder, file.Conflict.BaseId, basePath);

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

			gitService.GetFile(workingFolder, file.Conflict.BaseId, basePath);
			gitService.GetFile(workingFolder, file.Conflict.TheirsId, theirsPath);

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


		//public bool IsUseYours(string workingFolder, CommitFile file)
		//{
		//	string path = GetPath(workingFolder, file, UseYours);
		//	return File.Exists(path);
		//}

		//public bool IsUseTheirs(string workingFolder, CommitFile file)
		//{
		//	string path = GetPath(workingFolder, file, UseTheirs);
		//	return File.Exists(path);
		//}

		//public bool IsUseBase(string workingFolder, CommitFile file)
		//{
		//	string path = GetPath(workingFolder, file, UseBase);
		//	return File.Exists(path);
		//}

		//public bool IsDeleted(string workingFolder, CommitFile file)
		//{
		//	string path = GetPath(workingFolder, file, Deleted);
		//	return File.Exists(path);
		//}

		//public bool IsMerged(string workingFolder, CommitFile file)
		//{
		//	string path = GetPath(workingFolder, file, Merged);
		//	return File.Exists(path);
		//}


		public IReadOnlyList<string> GetAllTempNames()
		{
			List<string> names = new List<string>();

			names.Add(Base);
			names.Add(Yours);
			names.Add(Theirs);
			//names.Add(Deleted);
			//names.Add(Merged);
			//names.Add(UseBase);
			//names.Add(UseYours);
			//names.Add(UseTheirs);
			return names;
		}

		
		private void CleanTempPaths(string workingFolder, CommitFile file)
		{
			DeletePath(GetPath(workingFolder, file, Base));
			DeletePath(GetPath(workingFolder, file, Yours));
			DeletePath(GetPath(workingFolder, file, Theirs));
			//DeletePath(GetPath(workingFolder, file, Deleted));
			//DeletePath(GetPath(workingFolder, file, Merged));
			//DeletePath(GetPath(workingFolder, file, UseBase));
			//DeletePath(GetPath(workingFolder, file, UseYours));
			//DeletePath(GetPath(workingFolder, file, UseTheirs));
		}


		private void UseFile(string workingFolder, CommitFile file, string fileId)
		{
			string fullPath = Path.Combine(workingFolder, file.Path);

			gitService.GetFile(workingFolder, fileId, fullPath);
		}


		public async Task ShowFileDiffAsync(string workingFolder, string commitId, string name)
		{
			string p4mergeExe;
			if (!IsDiffSupported(out p4mergeExe))
			{
				return;
			}

			R<CommitDiff> commitDiff = await gitService.GetFileDiffAsync(workingFolder, commitId, name);

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
				MessageBox.Show(
					"Could not locate compatible diff tool.\nPlease install Perforce p4merge.",
					ProgramPaths.ProgramName,
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return false;
			}

			return true;
		}


		//private static void MarkAs(string workingFolder, CommitFile file, string mark)
		//{
		//	string path = GetPath(workingFolder, file, mark);
		//	File.AppendAllText(path, $"File set to {mark}.");
		//}


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
				&& !File.ReadAllLines(fullPath).Any(line => line.StartsWith(ConflictMarker));
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