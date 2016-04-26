using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using GitMind.CommitsHistory;
using GitMind.Installation;
using GitMind.Settings;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind
{
	internal class MainWindowViewModel : ViewModel
	{
		private readonly IDiffService diffService;
		private readonly ILatestVersionService latestVersionService;
		private readonly Window owner;
		private readonly Func<Task> refreshAsync;


		internal MainWindowViewModel(
			IHistoryViewModel historyViewModelViewModel,
			IDiffService diffService,
			ILatestVersionService latestVersionService,
			Window owner,
			Func<Task> refreshAsync)
		{
			HistoryViewModel = historyViewModelViewModel;
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
				Set(value);
				SetSearchBoxValue(value);
			}
		}


		private void SetSearchBoxValue(string text)
		{
			HistoryViewModel.SetFilter(text);
		}


		public BusyIndicator Busy => BusyIndicator();

		public IHistoryViewModel HistoryViewModel { get; }


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

		public Command MinimizeCommand => Command(Minimize);

		public Command CloseCommand => Command(CloseWindow);

		public Command EscapeCommand => Command(Escape);


		private void Escape()
		{
			if (!string.IsNullOrWhiteSpace(SearchBox))
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


		private async void ShowDiff()
		{
			await diffService.ShowDiffAsync(null);
		}


		private async void SelectWorkingFolder()
		{
			List<string> activeBranches = new List<string>();
			HistoryViewModel.SetBranches(activeBranches);

			var dialog = new System.Windows.Forms.FolderBrowserDialog();
			dialog.Description = "Select a working folder.";
			dialog.ShowNewFolderButton = false;
			dialog.SelectedPath = Environment.CurrentDirectory;
			if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}

			Environment.CurrentDirectory = dialog.SelectedPath;

			await HistoryViewModel.LoadAsync(owner);

			WorkingFolder = ProgramPaths.GetWorkingFolderPath(Environment.CurrentDirectory).Or("");
		}
	}
}