﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;
using GitMind.Features.FolderMonitoring;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Installation;
using GitMind.Installation.Private;
using GitMind.RepositoryViews;
using GitMind.Settings;
using GitMind.Utils;
using GitMind.Utils.UI;
using Application = System.Windows.Application;


namespace GitMind.MainWindowViews
{
	internal class MainWindowViewModel : ViewModel
	{
		private readonly IDiffService diffService = new DiffService();
		private readonly IGitService gitService = new GitService();

		private readonly ILatestVersionService latestVersionService = new LatestVersionService();
		private readonly FolderMonitorService folderMonitor;

		private readonly Window owner;
		private readonly Action setSearchFocus;
		private readonly Action setRepositoryViewFocus;
		private bool isLoaded = false;

		//private bool isStatusChanged = false;
		//private bool isRepositoryChanged = false;


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

		//public Command ShowUncommittedDiffCommand => Command(ShowUncommittedDiff, IsUncommitted);

		//		public Command CommitCommand => Command(CommitChanges, IsUncommitted);



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
				ProgramSettings.SetLatestUsedWorkingFolderPath(path.Value);

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

		private bool IsUncommitted()
		{
			return RepositoryViewModel.UnCommited != null;
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


		public Task StatusChangeRefreshAsync(DateTime triggerTime, bool isRepoChange)
		{
			if (!isLoaded)
			{
				return Task.CompletedTask;
			}

			//if (owner.WindowState == WindowState.Minimized || !VisibleWindow.IsVisible(owner))
			//{
			//	Log.Debug("Not visible");
			//	isStatusChanged = true;
			//	isRepositoryChanged = isRepositoryChanged || isRepoChange;
			//	return Task.CompletedTask;
			//}

			//isStatusChanged = false;
			//isRepositoryChanged = false;
			return RepositoryViewModel.StatusChangeRefreshAsync(triggerTime, isRepoChange);
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


		public IReadOnlyList<string> SpecifiedBranchNames
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