using System.IO;
using System.Threading.Tasks;
using System.Windows;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Settings;
using GitMind.Utils;


namespace GitMind.CommitsHistory
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


		public async Task ShowDiffAsync(string commitId)
		{
			string p4mergeExe = "C:\\Program Files\\Perforce\\p4merge.exe";

			if (!File.Exists(p4mergeExe))
			{
				MessageBox.Show(
					"Could not locate compatible diff tool.\nPlease install Perforce p4merge.",
						ProgramPaths.ProgramName,
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
				return;
			}

			Result<CommitDiff> commitDiff = await gitService.GetCommitDiffAsync(commitId);

			if (commitDiff.HasValue)
			{
				await Task.Run(() =>
				{
					cmd.Run(p4mergeExe, $"\"{commitDiff.Value.LeftPath}\" \"{commitDiff.Value.RightPath}\"");
				});
			}
		}
	}
}