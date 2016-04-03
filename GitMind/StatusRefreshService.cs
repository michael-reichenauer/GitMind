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
				GitStatus status = await gitService.GetStatusAsync(null);

				string statusText = null;

				if (!status.OK)
				{
					int count = status.Added + status.Deleted + status.Modified + status.Other;
					statusText = $"  Uncommitted: {count}";
				}

				mainWindowViewModel.StatusText.Value = statusText;
				mainWindowViewModel.IsStatusVisible.Value = !string.IsNullOrWhiteSpace(statusText);

				string currentBranchName = await gitService.GetCurrentBranchNameAsync(null);

				mainWindowViewModel.BranchName.Value = currentBranchName;
			}
			catch (Exception ex)
			{
				Log.Warn($"Failed to update status {ex}");
			}
		}


		private void UpdateStatus(object sender, EventArgs e)
		{
			UpdateStatusAsync().RunInBackground();
		}
	}
}