using System;
using System.Diagnostics;
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
using MessageBox = System.Windows.MessageBox;


namespace GitMind
{
	internal class MainWindowViewModel : ViewModel
	{
		private readonly IRepositoryService repositoryService;
		private readonly IDiffService diffService;
		private readonly ILatestVersionService latestVersionService;
		private readonly Window owner;
		private readonly Func<Task> refreshAsync;


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

		public string WorkingFolder
		{
			get { return Get(); }
			set { Set(value); }
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

		public Command InstallLatestVersionCommand => Command(InstallLatestVersion);

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
			if (RepositoryViewModel.DetailsSize > 0)
			{
				RepositoryViewModel.DetailsSize = 0;
			}
			else if (!string.IsNullOrWhiteSpace(SearchBox))
			{
				SearchBox = "";
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

		private async void InstallLatestVersion()
		{
			//if (MessageBoxResult.OK != MessageBox.Show(
			//	Application.Current.MainWindow,
			//	"There is a new version of GitMind.\n\n" +
			//	"Would you like to download and install the new version?",
			//	"GitMind",
			//	MessageBoxButton.OKCancel,
			//	MessageBoxImage.Question))
			//{
			//	return;
			//}

			bool isInstalling = await latestVersionService.InstallLatestVersionAsync();

			if (isInstalling)
			{
				// Newer version is being installed and will run, close this instance
				Application.Current.Shutdown(0);
			}
			else
			{
				MessageBox.Show(
					Application.Current.MainWindow,
					"Failed to install newer version.",
					"GitMind",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
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
			await diffService.ShowDiffAsync(null);
		}


		private async void SelectWorkingFolder()
		{
			string selectedPath;
			while (true)
			{
				var dialog = new FolderBrowserDialog();
				dialog.Description = "Select a working folder with a valid git repository.";
				dialog.ShowNewFolderButton = false;
				dialog.SelectedPath = Environment.CurrentDirectory;
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
			Environment.CurrentDirectory = selectedPath;

			Task<Repository> repositoryTask = repositoryService.GetRepositoryAsync(true);

			Busy.Add(repositoryTask);

			Repository repository = await repositoryTask;

			RepositoryViewModel.Update(repository, new string[0]);

			WorkingFolder = ProgramPaths.GetWorkingFolderPath(Environment.CurrentDirectory).Or("");
		}


		private async void SpecifyCommitBranch()
		{
			var commit = RepositoryViewModel.SelectedItem as CommitViewModel;
			if (commit != null)
			{
				await commit.SetCommitBranchCommand.ExecuteAsync(null);
			}
		}

	}
}