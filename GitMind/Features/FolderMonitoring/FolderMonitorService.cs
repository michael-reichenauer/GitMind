using System;
using System.IO;
using System.Windows.Threading;
using GitMind.Utils;


namespace GitMind.Features.FolderMonitoring
{
	public class FolderMonitorService
	{
		private const string GitFolder = ".git";
		private const string GitRefsFolder = "refs";
		private static readonly string GitHeadFile = Path.Combine(GitFolder, "HEAD");
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
		private string currentFolder = null;


		public FolderMonitorService(Action statusTriggerAction, Action repoTriggerAction)
		{
			this.statusTriggerAction = statusTriggerAction;
			statusTimer = new DispatcherTimer();
			statusTimer.Tick += (s, e) => OnStatusTimer();
			statusTimer.Interval = MinTriggerTimeout;
			workFolderWatcher.Changed += (s, e) => WorkingFolderChange(e.FullPath, e.Name, e.ChangeType);
			workFolderWatcher.Created += (s, e) => WorkingFolderChange(e.FullPath, e.Name, e.ChangeType);
			workFolderWatcher.Deleted += (s, e) => WorkingFolderChange(e.FullPath, e.Name, e.ChangeType);
			workFolderWatcher.Renamed += (s, e) => WorkingFolderChange(e.FullPath, e.Name, e.ChangeType);

			this.repoTriggerAction = repoTriggerAction;
			repoTimer = new DispatcherTimer();
			repoTimer.Tick += (s, e) => OnRepoTimer();
			repoTimer.Interval = MinTriggerTimeout;
			refsWatcher.Changed += (s, e) => RepoChange(e.FullPath, e.Name, e.ChangeType);
			refsWatcher.Created += (s, e) => RepoChange(e.FullPath, e.Name, e.ChangeType);
			refsWatcher.Deleted += (s, e) => RepoChange(e.FullPath, e.Name, e.ChangeType);
			refsWatcher.Renamed += (s, e) => RepoChange(e.FullPath, e.Name, e.ChangeType);
		}


		public void Monitor(string workingFolder)
		{
			currentFolder = workingFolder;
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


		private void WorkingFolderChange(string fullPath, string path, WatcherChangeTypes changeType)
		{
			if (path == GitHeadFile)
			{
				RepoChange(fullPath, path, changeType);
				return;
			}

			if (path == null || !path.StartsWith(GitFolder))
			{
				// Log.Debug($"Status chage for '{fullPath}' {changeType}");
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


		private void RepoChange(string fullPath, string path, WatcherChangeTypes changeType)
		{
			if (Path.GetExtension(fullPath) == ".lock")
			{
				return;
			}
			else if (Directory.Exists(fullPath))
			{
				return;
			}

			Log.Debug($"Repo change for '{fullPath}' {changeType}");

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
				statusTriggerTime = now;
				statusChangeTime = now;

				repoTriggerTime = now;
				repoChangeTime = now;
				repoTriggerAction();
			}

			if (now - repoChangeTime > EndTriggerTimeout)
			{
				repoTimer.Stop();
				statusTimer.Stop();

				bool isEndTrigger = repoChangeTime > repoTriggerTime;

				statusTriggerTime = DateTime.MinValue;
				statusChangeTime = now;

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