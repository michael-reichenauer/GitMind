using System;
using System.IO;
using System.Windows.Threading;
using GitMind.Utils;


namespace GitMind.Features.StatusHandling.Private
{
	[SingleInstance]
	internal class FolderMonitorService : IFolderMonitorService
	{
		private static readonly TimeSpan StatusDelayTriggerTime = TimeSpan.FromSeconds(1);
		private static readonly TimeSpan RepositoryDelayTriggerTime = TimeSpan.FromSeconds(1);

		private const string GitFolder = ".git";
		private const string GitRefsFolder = "refs";
		private static readonly string GitHeadFile = Path.Combine(GitFolder, "HEAD");
		private const NotifyFilters NotifyFilters =
			System.IO.NotifyFilters.LastWrite
			| System.IO.NotifyFilters.FileName
			| System.IO.NotifyFilters.DirectoryName;

		private readonly FileSystemWatcher workFolderWatcher = new FileSystemWatcher();
		private readonly FileSystemWatcher refsWatcher = new FileSystemWatcher();

		private LibGit2Sharp.Repository repo = null;

		private readonly object syncRoot = new object();

		private readonly DispatcherTimer statusTimer;
		private bool isStatus = false;
		private DateTime statusChangeTime;

		private readonly DispatcherTimer repoTimer;
		private bool isRepo = false;
		private DateTime repoChangeTime;


		public FolderMonitorService()
		{
			statusTimer = new DispatcherTimer();
			statusTimer.Tick += (s, e) => OnStatusTimer();
			statusTimer.Interval = StatusDelayTriggerTime;

			workFolderWatcher.Changed += (s, e) => WorkingFolderChange(e.FullPath, e.Name, e.ChangeType);
			workFolderWatcher.Created += (s, e) => WorkingFolderChange(e.FullPath, e.Name, e.ChangeType);
			workFolderWatcher.Deleted += (s, e) => WorkingFolderChange(e.FullPath, e.Name, e.ChangeType);
			workFolderWatcher.Renamed += (s, e) => WorkingFolderChange(e.FullPath, e.Name, e.ChangeType);

			repoTimer = new DispatcherTimer();
			repoTimer.Tick += (s, e) => OnRepoTimer();
			repoTimer.Interval = RepositoryDelayTriggerTime;

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
			repoChangeTime = DateTime.Now;
		
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
			lock (syncRoot)
			{
				isStatus = true;
				statusChangeTime = DateTime.Now;

				if (!statusTimer.IsEnabled)
				{
					statusTimer.Start();
				}
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

			lock (syncRoot)
			{
				isRepo = true;
				repoChangeTime = DateTime.Now;

				if (!repoTimer.IsEnabled)
				{
					repoTimer.Start();
				}
			}
		}


		private void OnStatusTimer()
		{
			lock (syncRoot)
			{
				if (!isStatus)
				{
					statusTimer.Stop();
					return;
				}

				isStatus = false;
			}
		
			FileChanged?.Invoke(this, new FileEventArgs(statusChangeTime));
		}


		private void OnRepoTimer()
		{
			lock (syncRoot)
			{
				if (!isRepo)
				{
					repoTimer.Stop();
					return;
				}

				isRepo = false;
			}

			RepoChanged?.Invoke(this, new FileEventArgs(repoChangeTime));		
		}
	}
}