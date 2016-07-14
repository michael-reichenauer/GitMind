using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using GitMind.CommitsHistory;
using GitMind.GitModel;
using GitMind.Installation;
using GitMind.Settings;
using GitMind.Utils;
using GitMind.Utils.UI;
using Application = System.Windows.Application;


namespace GitMind
{
	internal class MainWindowViewModel : ViewModel
	{
		private readonly IRepositoryService repositoryService;
		private readonly IDiffService diffService;
		private readonly ILatestVersionService latestVersionService;
		private readonly Window owner;
		private readonly Func<Task> refreshAsync;
		private readonly FileSystemWatcher watcher = new FileSystemWatcher();
		private string watchedPath = null;

		internal MainWindowViewModel(
			RepositoryViewModel repositoryViewModel,
			IRepositoryService repositoryService,
			IDiffService diffService,
			ILatestVersionService latestVersionService,
			Window owner,
			Func<Task> refreshAsync)
		{
			RepositoryViewModel = repositoryViewModel;
			this.repositoryService = repositoryService;
			this.diffService = diffService;
			this.latestVersionService = latestVersionService;
			this.owner = owner;
			this.refreshAsync = refreshAsync;
		}



		public string StatusText
		{
			get { return Get(); }
			set { Set(value).Notify(nameof(IsStatusVisible)); }
		}

		public bool IsStatusVisible
		{
			get { return Get(); }
			set { Set(value); }
		}

		public bool IsInFilterMode => !string.IsNullOrEmpty(SearchBox);


		public bool IsNewVersionVisible
		{
			get { return Get(); }
			set { Set(value); }
		}


		public string BranchName
		{
			get { return Get(); }
			set { Set(value); }
		}

		public Brush BranchBrush
		{
			get { return Get(); }
			set { Set(value); }
		}


		public string WorkingFolder
		{
			get { return Get(); }
			set
			{
				Set(value);
				//AddFolderWatcher(value);
			}
		}




		public string SearchBox
		{
			get { return Get(); }
			set
			{
				Set(value).Notify(nameof(IsInFilterMode));
				SetSearchBoxValue(value);
			}
		}


		private void SetSearchBoxValue(string text)
		{
			RepositoryViewModel.SetFilter(text);
		}


		public BusyIndicator Busy => BusyIndicator();

		public RepositoryViewModel RepositoryViewModel { get; }


		public string VersionText
		{
			get
			{
				Version version = ProgramPaths.GetCurrentVersion();
				DateTime buildTime = ProgramPaths.BuildTime();
				string dateText = buildTime.ToString("yyyy-MM-dd\nHH:mm");
				string text = $"Version: {version.Major}.{version.Minor}\n{dateText}";
				return text;
			}
		}


		public Command SelectWorkingFolderCommand => Command(SelectWorkingFolder);

		public Command ShowDiffCommand => Command(ShowDiff);

		public Command RunLatestVersionCommand => Command(RunLatestVersion);

		public Command FeedbackCommand => Command(Feedback);

		public Command HelpCommand => Command(OpenHelp);

		public Command MinimizeCommand => Command(Minimize);

		public Command CloseCommand => Command(CloseWindow);

		public Command ToggleMaximizeCommand => Command(ToggleMaximize);

		public Command EscapeCommand => Command(Escape);

		public Command ClearFilterCommand => Command(ClearFilter);

		public Command SpecifyCommitBranchCommand => Command(SpecifyCommitBranch);




		private void Escape()
		{
			if (!string.IsNullOrWhiteSpace(SearchBox))
			{
				SearchBox = "";
			}
			else if (RepositoryViewModel.DetailsSize > 0)
			{
				RepositoryViewModel.DetailsSize = 0;
			}
			else
			{
				CloseWindow();
			}
		}


		public Command RefreshCommand => AsyncCommand(Refresh);


		private async Task Refresh()
		{
			Task refreshTask = refreshAsync();
			Busy.Add(refreshTask);
			await refreshTask;
		}


		private void Minimize()
		{
			Application.Current.MainWindow.WindowState = WindowState.Minimized;
		}


		private void ToggleMaximize()
		{
			if (Application.Current.MainWindow.WindowState == WindowState.Maximized)
			{
				Application.Current.MainWindow.WindowState = WindowState.Normal;
			}
			else
			{
				Application.Current.MainWindow.WindowState = WindowState.Maximized;
			}
		}

		private void CloseWindow()
		{
			Application.Current.Shutdown(0);
		}

		private async void RunLatestVersion()
		{
			bool isInstalling = await latestVersionService.RunLatestVersionAsync();

			if (isInstalling)
			{
				// Newer version is being installed and will run, close this instance
				Application.Current.Shutdown(0);
			}
		}


		private void Feedback()
		{
			try
			{
				Process proc = new Process();
				proc.StartInfo.FileName = "mailto:michael.reichenauer@gmail.com&subject=GitMind Feedback";
				proc.Start();
			}
			catch (Exception ex) when (ex.IsNotFatal())
			{
				Log.Error($"Failed to open feedback link {ex}");
			}
		}


		private void OpenHelp()
		{
			try
			{
				Process proc = new Process();
				proc.StartInfo.FileName = "https://github.com/michael-reichenauer/GitMind/wiki/Help";
				proc.Start();
			}
			catch (Exception ex) when (ex.IsNotFatal())
			{
				Log.Error($"Failed to open help link {ex}");
			}
		}

		private void ClearFilter()
		{
			if (!string.IsNullOrWhiteSpace(SearchBox))
			{
				SearchBox = "";
			}
		}


		private async void ShowDiff()
		{
			await diffService.ShowDiffAsync(null, WorkingFolder);
		}


		private async void SelectWorkingFolder()
		{
			string selectedPath;
			while (true)
			{
				var dialog = new FolderBrowserDialog();
				dialog.Description = "Select a working folder with a valid git repository.";
				dialog.ShowNewFolderButton = false;
				dialog.RootFolder = Environment.SpecialFolder.MyComputer;
				dialog.SelectedPath = WorkingFolder ?? Environment.CurrentDirectory;
				if (dialog.ShowDialog(owner.GetIWin32Window()) != DialogResult.OK)
				{
					Log.Warn("User canceled selecting a Working folder");
					return;
				}

				R<string> workingFolder = ProgramPaths.GetWorkingFolderPath(dialog.SelectedPath);
				if (workingFolder.HasValue)
				{
					Log.Warn($"User selected valid {workingFolder.Value}");
					selectedPath = workingFolder.Value;
					break;
				}
				else
				{
					Log.Warn($"User selected an invalid working folder: {dialog.SelectedPath}");
				}
			}

			Log.Debug($"Setting working folder {selectedPath}");
			ProgramSettings.SetLatestUsedWorkingFolderPath(selectedPath);
			WorkingFolder = selectedPath;

			Task<Repository> repositoryTask = repositoryService.GetRepositoryAsync(true, selectedPath);

			Busy.Add(repositoryTask);

			Repository repository = await repositoryTask;

			RepositoryViewModel.Update(repository, new string[0]);			
		}


		private async void SpecifyCommitBranch()
		{
			var commit = RepositoryViewModel.SelectedItem as CommitViewModel;
			if (commit != null)
			{
				await commit.SetCommitBranchCommand.ExecuteAsync(null);
			}
		}


		private void AddFolderWatcher(string path)
		{
			Log.Warn("Called");
			if (string.IsNullOrEmpty(path))
			{
				watcher.EnableRaisingEvents = false;
				watchedPath = null;
				Log.Warn("Disable watcher");
				return;
			}

			if (!path.EndsWith(".git"))
			{
				path = Path.Combine(path, ".git");
			}

			if (path == watchedPath)
			{
				Log.Warn($"Already watching {path}");
				return;
			}


			Log.Warn($"Watching {path}");
			watchedPath = path;
			watcher.Path = path;
			watcher.IncludeSubdirectories = true;


			watcher.NotifyFilter = NotifyFilters.LastWrite
				 | NotifyFilters.FileName | NotifyFilters.DirectoryName;
			
			watcher.Filter = "*.lock";

			// Add event handlers.
			watcher.Changed += OnFileChanged;
			watcher.Created += OnFileChanged;
			watcher.Deleted += OnFileChanged;
			watcher.Renamed += OnFileRenamed;
			watcher.Error += OnFileError;

			// Begin watching.
			watcher.EnableRaisingEvents = true;
		}


		private void OnFileError(object sender, ErrorEventArgs e)
		{
			Log.Warn($"Error: {e.GetException()}");
		}


		private static void OnFileChanged(object source, FileSystemEventArgs e)
		{
			// Specify what is done when a file is changed, created, or deleted.
			Log.Warn($"File: { e.FullPath} {e.ChangeType}");
		}

		private static void OnFileRenamed(object source, RenamedEventArgs e)
		{
			// Specify what is done when a file is renamed.
			Log.Warn($"File: {e.OldFullPath} renamed to {e.FullPath}");
		}
	}
}