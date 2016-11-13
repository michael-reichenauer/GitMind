﻿using System;
using System.IO;
using System.Windows.Threading;
using GitMind.Utils;


namespace GitMind.Features.StatusHandling.Private
{
	[SingleInstance]
	internal class FolderMonitorService : IFolderMonitorService
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
		private readonly DispatcherTimer statusTimer;

		private DateTime repoChangeTime;
		private DateTime repoTriggerTime;
		private readonly DispatcherTimer repoTimer;

		private LibGit2Sharp.Repository repo = null;


		public FolderMonitorService()
		{
			statusTimer = new DispatcherTimer();
			statusTimer.Tick += (s, e) => OnStatusTimer();
			statusTimer.Interval = MinTriggerTimeout;
			workFolderWatcher.Changed += (s, e) => WorkingFolderChange(e.FullPath, e.Name, e.ChangeType);
			workFolderWatcher.Created += (s, e) => WorkingFolderChange(e.FullPath, e.Name, e.ChangeType);
			workFolderWatcher.Deleted += (s, e) => WorkingFolderChange(e.FullPath, e.Name, e.ChangeType);
			workFolderWatcher.Renamed += (s, e) => WorkingFolderChange(e.FullPath, e.Name, e.ChangeType);

			repoTimer = new DispatcherTimer();
			repoTimer.Tick += (s, e) => OnRepoTimer();
			repoTimer.Interval = MinTriggerTimeout;
			refsWatcher.Changed += (s, e) => RepoChange(e.FullPath, e.Name, e.ChangeType);
			refsWatcher.Created += (s, e) => RepoChange(e.FullPath, e.Name, e.ChangeType);
			refsWatcher.Deleted += (s, e) => RepoChange(e.FullPath, e.Name, e.ChangeType);
			refsWatcher.Renamed += (s, e) => RepoChange(e.FullPath, e.Name, e.ChangeType);
		}


		public event EventHandler<FileEventArgs> FileChanged;

		public event EventHandler<FileEventArgs> RepoChanged;

		public void Monitor(string workingFolder)
		{
			string refsPath = Path.Combine(workingFolder, GitFolder, GitRefsFolder);
			if (!Directory.Exists(workingFolder) || !Directory.Exists(refsPath))
			{
				Log.Debug("Selected folder is not a root working folder.");
				return;
			}

			workFolderWatcher.EnableRaisingEvents = false;
			refsWatcher.EnableRaisingEvents = false;
			statusTimer.Stop();
			repoTimer.Stop();

			repo = GetRepo(workingFolder);

			workFolderWatcher.Path = workingFolder;
			workFolderWatcher.NotifyFilter = NotifyFilters;
			workFolderWatcher.Filter = "*.*";
			workFolderWatcher.IncludeSubdirectories = true;


			refsWatcher.Path = refsPath;
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


		private LibGit2Sharp.Repository GetRepo(string workingFolder)
		{
			try
			{
				if (repo != null)
				{
					repo.Dispose();
				}

				return new LibGit2Sharp.Repository(workingFolder);
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to create repo to check ignored files, {e}");
			}

			return null;
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
				if (repo != null && path != null && repo.Ignore.IsPathIgnored(path))
				{
					return;
				}

				if (fullPath != null && !Directory.Exists(fullPath))
				{
					Log.Debug($"Status change for '{fullPath}' {changeType}");
					StatusChange();
				}
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
			DateTime triggerTime = statusChangeTime;

			if (now - statusTriggerTime > MaxTriggerTimeout)
			{
				statusTriggerTime = now;
				statusChangeTime = now;
				FileChanged?.Invoke(this, new FileEventArgs(triggerTime));
			}

			if (now - statusChangeTime > EndTriggerTimeout)
			{
				statusTimer.Stop();

				bool isEndTrigger = statusChangeTime > statusTriggerTime;

				statusTriggerTime = DateTime.MinValue;
				statusChangeTime = now;

				if (isEndTrigger)
				{
					FileChanged?.Invoke(this, new FileEventArgs(triggerTime));
				}
			}
		}


		private void OnRepoTimer()
		{
			DateTime now = DateTime.Now;
			DateTime triggerTime = repoChangeTime;

			if (now - repoTriggerTime > MaxTriggerTimeout)
			{
				statusTriggerTime = now;
				statusChangeTime = now;

				repoTriggerTime = now;
				repoChangeTime = now;
				RepoChanged?.Invoke(this, new FileEventArgs(triggerTime));
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
					RepoChanged?.Invoke(this, new FileEventArgs(triggerTime));
				}
			}
		}
	}
}