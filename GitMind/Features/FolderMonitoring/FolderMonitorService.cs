using System;
using System.IO;
using System.Threading;
using GitMind.Utils;


namespace GitMind.Features.FolderMonitoring
{
	public class FolderMonitorService
	{
		private readonly FileSystemWatcher watcher = new FileSystemWatcher();
		private DateTime changedFiles;
		private DateTime changedRepo;

		private Timer timer = new Timer(OnTimer);




		public FolderMonitorService()
		{
			
		}


		public void Start(string workingFolder)
		{
			watcher.Path = workingFolder;

			watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.LastAccess
					| NotifyFilters.FileName | NotifyFilters.DirectoryName;
			watcher.Filter = "*.*";
			watcher.IncludeSubdirectories = true;

			// Add event handlers.
			watcher.Changed += OnChanged;
			watcher.Created += OnChanged;
			watcher.Deleted += OnChanged;
			watcher.Renamed += OnRenamed;

			// Begin watching.
			changedFiles = DateTime.Now;
			changedRepo = DateTime.Now;
			watcher.EnableRaisingEvents = true;
		}


		private void OnChanged(object source, FileSystemEventArgs e)
		{
			CheckChange(e.Name);
		}

		private void OnRenamed(object sender, RenamedEventArgs e)
		{
			CheckChange(e.Name);
		}


		private void CheckChange(string name)
		{
			DateTime now = DateTime.Now;
			if (name.StartsWith(".git\\refs") && name == ".git\\HEAD")
			{
				Log.Debug($"Repository change {name}");
				
				if (now - changedRepo > TimeSpan.FromMilliseconds(1000))
				{
					timer.Change(500, -1);
				}

				changedRepo = now;
			}
			else if (!name.StartsWith(".git"))
			{
				Log.Debug($"Folder change {name}");
			}
		}

		private static void OnTimer(object state)
		{
			//if (name.StartsWith(".git\\refs") && name == ".git\\HEAD")
			//{
			//	Log.Debug($"Repository change {name}");

			//	if (now - changedRepo > TimeSpan.FromMilliseconds(1000))
			//	{
			//		timer.Change(500, -1);
			//	}

			//	changedRepo = now;
			//}
			//else if (!name.StartsWith(".git"))
			//{
			//	Log.Debug($"Folder change {name}");
			//}
		}
	}
}