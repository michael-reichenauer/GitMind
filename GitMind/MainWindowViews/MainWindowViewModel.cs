using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using GitMind.Common;
using GitMind.Features.FolderMonitoring;
using GitMind.Git;
using GitMind.Installation;
using GitMind.Installation.Private;
using GitMind.RepositoryViews;
using GitMind.SettingsHandling;
using GitMind.Utils;
using GitMind.Utils.UI;
using Application = System.Windows.Application;


namespace GitMind.MainWindowViews
{
	internal class MainWindowViewModel : ViewModel
	{
		private readonly ILatestVersionService latestVersionService = new LatestVersionService();
		private readonly FolderMonitorService folderMonitor;
		private readonly JumpListService jumpListService = new JumpListService();

		private readonly Window owner;
		private readonly Action setSearchFocus;
		private readonly Action setRepositoryViewFocus;
		private bool isLoaded = false;
		private IpcRemotingService ipcRemotingService = new IpcRemotingService();


		internal MainWindowViewModel(
			Window owner,
			Action setSearchFocus,
			Action setRepositoryViewFocus)
		{
			RepositoryViewModel = new RepositoryViewModel(owner, Busy);
			this.owner = owner;
			this.setSearchFocus = setSearchFocus;
			this.setRepositoryViewFocus = setRepositoryViewFocus;
			folderMonitor = new FolderMonitorService(OnStatusChange, OnRepoChange);
		}


		public bool IsInFilterMode => !string.IsNullOrEmpty(SearchBox);


		public bool IsNewVersionVisible
		{
			get { return Get(); }
			set { Set(value); }
		}


		public string WorkingFolder
		{
			get { return Get(); }
			set
			{
				if (Set(value).IsSet)
				{
					if (ipcRemotingService != null)
					{
						ipcRemotingService.Dispose();
					}

					ipcRemotingService = new IpcRemotingService();

					string id = MainWindowIpcService.GetId(value);
					if (ipcRemotingService.TryCreateServer(id))
					{
						ipcRemotingService.PublishService(new MainWindowIpcService(this));
					}
					else
					{
						// Another GitMind instance for that working folder is already running, activate that.
						ipcRemotingService.CallService<MainWindowIpcService>(id, service => service.Activate(null));
						Application.Current.Shutdown(0);
						ipcRemotingService.Dispose();
						return;
					}

					jumpListService.Add(value);
					RepositoryViewModel.WorkingFolder = value;
					folderMonitor.Monitor(value);
					Notify(nameof(Title));
				}
			}
		}


		public string Title => WorkingFolder != null
		? $"{Path.GetFileName(WorkingFolder)} - GitMind" : "GitMind";


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

		public Command RefreshCommand => AsyncCommand(ManualRefreshAsync);

		public Command SelectWorkingFolderCommand => Command(SelectWorkingFolder);

		public Command RunLatestVersionCommand => Command(RunLatestVersion);

		public Command FeedbackCommand => Command(Feedback);

		public Command HelpCommand => Command(OpenHelp);

		public Command MinimizeCommand => Command(Minimize);

		public Command CloseCommand => Command(CloseWindow);

		public Command ExitCommand => Command(Exit);

		public Command ToggleMaximizeCommand => Command(ToggleMaximize);

		public Command EscapeCommand => Command(Escape);

		public Command ClearFilterCommand => Command(ClearFilter);

		public Command SpecifyCommitBranchCommand => Command(SpecifyCommitBranch);

		public Command SearchCommand => Command(Search);




		public async Task FirstLoadAsync()
		{
			R<string> path = ProgramPaths.GetWorkingFolderPath(WorkingFolder);
			if (path.HasValue)
			{
				WorkingFolder = path.Value;
				ProgramSettings settings = Settings.Get<ProgramSettings>();
				settings.LastUsedWorkingFolder = path.Value;
				Settings.Set(settings);

				await RepositoryViewModel.FirstLoadAsync();
				isLoaded = true;
			}
			else
			{
				await Application.Current.Dispatcher.BeginInvoke(
					DispatcherPriority.Normal,
					new Action(async () =>
					{
						string selectedPath;
						if (!GetWorkingFolder(WorkingFolder, out selectedPath))
						{
							Application.Current.Shutdown(0);
							return;
						}

						WorkingFolder = selectedPath;

						await RepositoryViewModel.FirstLoadAsync();
						isLoaded = true;
					}));
			}
		}


		private void OnStatusChange(DateTime triggerTime)
		{
			Log.Debug("Status change");
			StatusChangeRefreshAsync(triggerTime, false).RunInBackground();
		}


		private void OnRepoChange(DateTime triggerTime)
		{
			Log.Debug("Repo change");
			StatusChangeRefreshAsync(triggerTime, true).RunInBackground();
		}


		private Task ManualRefreshAsync()
		{
			return RepositoryViewModel.ManualRefreshAsync();
		}

		public Task AutoRemoteCheckAsync()
		{
			return RepositoryViewModel.AutoRemoteCheckAsync();
		}


		private void Search()
		{
			setSearchFocus();
		}


		public async Task StatusChangeRefreshAsync(DateTime triggerTime, bool isRepoChange)
		{
			if (!isLoaded)
			{
				return;
			}

			Timing t = new Timing();

			await RepositoryViewModel.StatusChangeRefreshAsync(triggerTime, isRepoChange);
			t.Log($"Status change is repo change: {isRepoChange}");
		}


		public Task ActivateRefreshAsync()
		{
			if (!isLoaded)
			{
				return Task.CompletedTask;
			}


			return RepositoryViewModel.ActivateRefreshAsync();
		}


		private void Escape()
		{
			if (!string.IsNullOrWhiteSpace(SearchBox))
			{
				SearchBox = "";
				setRepositoryViewFocus();
			}
			else if (RepositoryViewModel.IsShowCommitDetails)
			{
				RepositoryViewModel.IsShowCommitDetails = false;
				setRepositoryViewFocus();
			}
			else
			{
				Minimize();
			}
		}


		public IReadOnlyList<BranchName> SpecifiedBranchNames
		{
			set { RepositoryViewModel.SpecifiedBranchNames = value; }
		}


		public int WindowWith
		{
			set { RepositoryViewModel.Width = value; }
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

		private void Exit()
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
				setRepositoryViewFocus();
			}
		}


		private async void SelectWorkingFolder()
		{
			isLoaded = false;
			string selectedPath;
			if (!GetWorkingFolder(WorkingFolder, out selectedPath))
			{
				isLoaded = true;
				return;
			}

			WorkingFolder = selectedPath;

			await RepositoryViewModel.FirstLoadAsync();
			isLoaded = true;
		}


		public bool GetWorkingFolder(string currentFolder, out string selectedPath)
		{
			selectedPath = null;

			while (true)
			{
				var dialog = new FolderBrowserDialog();
				dialog.Description = "Select a working folder with a valid git repository.";
				dialog.ShowNewFolderButton = false;
				dialog.RootFolder = Environment.SpecialFolder.MyComputer;
				if (currentFolder != null)
				{
					dialog.SelectedPath = currentFolder;
				}


				if (dialog.ShowDialog(owner.GetIWin32Window()) != DialogResult.OK)
				{
					Log.Warn("User canceled selecting a Working folder");
					return false;
				}


				R<string> workingFolder = ProgramPaths.GetWorkingFolderPath(dialog.SelectedPath);
				if (workingFolder.HasValue)
				{
					Log.Debug($"User selected valid {workingFolder.Value}");
					selectedPath = workingFolder.Value;
					break;
				}
				else
				{
					Log.Warn($"User selected an invalid working folder: {dialog.SelectedPath}");
				}
			}

			Log.Info($"Setting working folder '{selectedPath}'");
			ProgramSettings settings = Settings.Get<ProgramSettings>();
			settings.LastUsedWorkingFolder = selectedPath;
			Settings.Set(settings);
			return true;
		}


		private async void SpecifyCommitBranch()
		{
			var commit = RepositoryViewModel.SelectedItem as CommitViewModel;
			if (commit != null)
			{
				await commit.SetCommitBranchCommand.ExecuteAsync();
			}
		}
	}
}