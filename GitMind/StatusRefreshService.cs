using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using GitMind.CommitsHistory;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;


namespace GitMind
{
	internal class StatusRefreshService : IStatusRefreshService
	{
		private readonly MainWindowViewModel mainWindowViewModel;
		private readonly IGitService gitService;
		private readonly IBrushService brushService;


		public StatusRefreshService(MainWindowViewModel mainWindowViewModel)
			: this(mainWindowViewModel, new GitService(), new BrushService())
		{			
		}


		public StatusRefreshService(
			MainWindowViewModel mainWindowViewModel,
			IGitService gitService,
			IBrushService brushService)
		{
			this.mainWindowViewModel = mainWindowViewModel;
			this.gitService = gitService;
			this.brushService = brushService;
		}


		public void Start()
		{
			DispatcherTimer newVersionTime = new DispatcherTimer();

			newVersionTime.Tick += UpdateStatus;
			newVersionTime.Interval = TimeSpan.FromSeconds(60);
			newVersionTime.Start();
		}


		public async Task UpdateStatusAsync(string workingFolder)
		{
			try
			{
				R<GitStatus> statusResult = await gitService.GetStatusAsync(workingFolder);
				if (statusResult.IsFaulted) return;

				GitStatus status = statusResult.Value;
				string statusText = null;

				if (!status.OK)
				{
					int count = status.Count;
					statusText = $"  Uncommitted: {count}";
				}

				mainWindowViewModel.StatusText = statusText;
				mainWindowViewModel.IsStatusVisible = !string.IsNullOrWhiteSpace(statusText);

				R<string> currentBranchName = await gitService.GetCurrentBranchNameAsync(
					mainWindowViewModel.WorkingFolder);
				if (currentBranchName.IsFaulted) return;

				mainWindowViewModel.BranchName = currentBranchName.Value;
				mainWindowViewModel.BranchBrush = brushService.GetBranchBrush(currentBranchName.Value);
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Warn($"Failed to update status {e}");
			}
		}


		private void UpdateStatus(object sender, EventArgs e)
		{
			UpdateStatusAsync(mainWindowViewModel.WorkingFolder).RunInBackground();
		}
	}
}