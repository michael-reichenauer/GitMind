using System;
using System.IO;
using System.Windows.Threading;


namespace GitMind.Features.FolderMonitoring
{
	public class FolderMonitorService
	{
		private const string GitFolder = ".git";
		private const string GitRefsFolder = "refs";
		private const string GitHeadFile = "HEAD";
		private const NotifyFilters NotifyFilters =
			System.IO.NotifyFilters.LastWrite
			| System.IO.NotifyFilters.FileName
			| System.IO.NotifyFilters.DirectoryName;
		private static readonly TimeSpan MinTriggerTimeout = TimeSpan.FromSeconds(1);
		private static readonly TimeSpan MaxTriggerTimeout = TimeSpan.FromSeconds(10);
		private static readonly TimeSpan EndTriggerTimeout = TimeSpan.FromSeconds(5);

		private readonly FileSystemWatcher workFolderWatcher = new FileSystemWatcher();
		private readonly FileSystemWatcher refsWatcher = new FileSystemWatcher();

		private DateTime statusChangeTime;
		private DateTime statusTriggerTime;
		private readonly Action statusTriggerAction;
		private readonly DispatcherTimer statusTimer;

		private DateTime repoChangeTime;
		private DateTime repoTriggerTime;
		private readonly Action repoTriggerAction;
		private readonly DispatcherTimer repoTimer;


		public FolderMonitorService(Action statusTriggerAction, Action repoTriggerAction)
		{
			this.statusTriggerAction = statusTriggerAction;
			statusTimer = new DispatcherTimer();
			statusTimer.Tick += (s, e) => OnStatusTimer();
			statusTimer.Interval = MinTriggerTimeout;
			workFolderWatcher.Changed += (s, e) => WorkingFolderChange(e.Name);
			workFolderWatcher.Created += (s, e) => WorkingFolderChange(e.Name);
			workFolderWatcher.Deleted += (s, e) => WorkingFolderChange(e.Name);
			workFolderWatcher.Renamed += (s, e) => WorkingFolderChange(e.Name);

			this.repoTriggerAction = repoTriggerAction;
			repoTimer = new DispatcherTimer();
			repoTimer.Tick += (s, e) => OnRepoTimer();
			repoTimer.Interval = MinTriggerTimeout;
			refsWatcher.Changed += (s, e) => RefsChange();
			refsWatcher.Created += (s, e) => RefsChange();
			refsWatcher.Deleted += (s, e) => RefsChange();
			refsWatcher.Renamed += (s, e) => RefsChange();
		}


		public void Monitor(string workingFolder)
		{
			workFolderWatcher.EnableRaisingEvents = false;
			refsWatcher.EnableRaisingEvents = false;
			statusTimer.Stop();
			repoTimer.Stop();

			workFolderWatcher.Path = workingFolder;
			workFolderWatcher.NotifyFilter = NotifyFilters;
			workFolderWatcher.Filter = "*.*";
			workFolderWatcher.IncludeSubdirectories = true;

			refsWatcher.Path = Path.Combine(workingFolder, GitFolder, GitRefsFolder);
			refsWatcher.NotifyFilter = NotifyFilters;
			refsWatcher.Filter = "*.*";
			refsWatcher.IncludeSubdirectories = true;

			statusChangeTime = DateTime.Now;
			statusTriggerTime = DateTime.MinValue;
			repoChangeTime = DateTime.Now;
			repoTriggerTime = DateTime.MinValue;
			workFolderWatcher.EnableRaisingEvents = true;
			refsWatcher.EnableRaisingEvents = true;
		}


		private void WorkingFolderChange(string name)
		{
			if (name == GitHeadFile)
			{
				RefsChange();
				return;
			}

			if (!name.StartsWith(GitFolder))
			{
				StatusChange();
			}
		}


		private void StatusChange()
		{
			DateTime now = DateTime.Now;

			if (now - statusChangeTime > MinTriggerTimeout)
			{
				statusChangeTime = now;
				statusTimer.Start();
			}
			else
			{
				statusChangeTime = DateTime.Now;
			}
		}


		private void RefsChange()
		{
			DateTime now = DateTime.Now;

			if (now - repoChangeTime > MinTriggerTimeout)
			{
				repoChangeTime = now;
				repoTimer.Start();
			}
			else
			{
				repoChangeTime = DateTime.Now;
			}
		}


		private void OnStatusTimer()
		{
			DateTime now = DateTime.Now;

			if (now - statusTriggerTime > MaxTriggerTimeout)
			{
				statusTriggerTime = now;
				statusChangeTime = now;
				statusTriggerAction();
			}

			if (now - statusChangeTime > EndTriggerTimeout)
			{
				statusTimer.Stop();

				bool isEndTrigger = statusChangeTime > statusTriggerTime;

				statusTriggerTime = DateTime.MinValue;
				statusChangeTime = now;

				if (isEndTrigger)
				{
					statusTriggerAction();
				}
			}
		}


		private void OnRepoTimer()
		{
			DateTime now = DateTime.Now;

			if (now - repoTriggerTime > MaxTriggerTimeout)
			{
				repoTriggerTime = now;
				repoChangeTime = now;
				repoTriggerAction();
			}

			if (now - repoChangeTime > EndTriggerTimeout)
			{
				repoTimer.Stop();

				bool isEndTrigger = repoChangeTime > repoTriggerTime;

				repoTriggerTime = DateTime.MinValue;
				repoChangeTime = now;

				if (isEndTrigger)
				{
					repoTriggerAction();
				}
			}
		}
	}
}