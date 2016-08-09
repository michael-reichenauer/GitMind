using System.IO;
using System.Threading.Tasks;
using System.Windows;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Settings;
using GitMind.Utils;


namespace GitMind.RepositoryViews
{
	internal class DiffService : IDiffService
	{
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


		public async Task MergeConflictsAsync(string workingFolder, string id, string path, GitConflict gitConflict)
		{
			string p4mergeExe;
			if (!IsDiffSupported(out p4mergeExe))
			{
				return;
			}

			string fullPath = Path.Combine(workingFolder, path);
			string extension = Path.GetExtension(fullPath);
			string yoursPath = Path.ChangeExtension(fullPath, "YOURS" + extension);
			string basePath = Path.ChangeExtension(fullPath, "BASE" + extension);
			string theirsPath = Path.ChangeExtension(fullPath, "THEIRS" + extension);

			gitService.GetFile(workingFolder, gitConflict.OursId, yoursPath);
			gitService.GetFile(workingFolder, gitConflict.TheirsId, theirsPath);
			gitService.GetFile(workingFolder, gitConflict.BaseId, basePath);

			if (File.Exists(yoursPath) && File.Exists(theirsPath) && File.Exists(basePath))
			{
				await Task.Run(() =>
				{
					cmd.Run(p4mergeExe, $"\"{basePath}\" \"{theirsPath}\"  \"{yoursPath}\" \"{fullPath}\"");
				});

				File.Delete(yoursPath);
				File.Delete(theirsPath);
				File.Delete(basePath);
			}
		}


		public Task ResolveAsync(string workingFolder, string path)
		{
			return gitService.ResolveAsync(workingFolder, path);
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
	}
}