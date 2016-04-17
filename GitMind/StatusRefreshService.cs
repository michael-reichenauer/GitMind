using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;


namespace GitMind
{
	internal class StatusRefreshService : IStatusRefreshService
	{
		private readonly MainWindowViewModel mainWindowViewModel;
		private readonly IGitService gitService;


		public StatusRefreshService(MainWindowViewModel mainWindowViewModel)
			: this(mainWindowViewModel, new GitService())
		{			
		}


		public StatusRefreshService(
			MainWindowViewModel mainWindowViewModel,
			IGitService gitService)
		{
			this.mainWindowViewModel = mainWindowViewModel;
			this.gitService = gitService;
		}


		public void Start()
		{
			DispatcherTimer newVersionTime = new DispatcherTimer();

			newVersionTime.Tick += UpdateStatus;
			newVersionTime.Interval = TimeSpan.FromSeconds(60);
			newVersionTime.Start();
		}


		public async Task UpdateStatusAsync()
		{
			try
			{
				Result<GitStatus> statusResult = await gitService.GetStatusAsync(null);
				if (statusResult.IsFaulted) return;

				GitStatus status = statusResult.Value;
				string statusText = null;

				if (!status.OK)
				{
					int count = status.Added + status.Deleted + status.Modified + status.Other;
					statusText = $"  Uncommitted: {count}";
				}

				mainWindowViewModel.StatusText.Set(statusText);
				mainWindowViewModel.IsStatusVisible.Set(!string.IsNullOrWhiteSpace(statusText));

				Result<string> currentBranchName = await gitService.GetCurrentBranchNameAsync(null);
				if (currentBranchName.IsFaulted) return;

				mainWindowViewModel.BranchName.Set(currentBranchName.Value);
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Warn($"Failed to update status {e}");
			}
		}


		private void UpdateStatus(object sender, EventArgs e)
		{
			UpdateStatusAsync().RunInBackground();
		}
	}
}