using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
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


		internal MainWindowViewModel(
			ILogViewModel logViewModelViewModel,
			IDiffService diffService,
			ILatestVersionService latestVersionService,
			Window owner)
		{
			LogViewModel = logViewModelViewModel;
			this.diffService = diffService;
			this.latestVersionService = latestVersionService;
			this.owner = owner;

			StatusText.WhenSetNotify(nameof(IsStatusVisible));
		}


		public Property<string> StatusText => Property<string>();

		public Property<bool> IsStatusVisible => Property<bool>();

		public Property<bool> IsNewVersionVisible => Property<bool>();

		public Property<string> BranchName => Property<string>();

		public Property<string> WorkingFolder => Property<string>();

		public ILogViewModel LogViewModel { get; }


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


		public ICommand SelectWorkingFolderCommand => Command(SelectWorkingFolder);

		public ICommand ShowDiffCommand => Command(ShowDiff);

		public ICommand InstallLatestVersionCommand => Command(InstallLatestVersion);

		public ICommand FeedbackCommand => Command(Feedback);


		private async void InstallLatestVersion()
		{
			if (MessageBoxResult.OK != MessageBox.Show(
				Application.Current.MainWindow,
				"There is a new version of GitMind.\n\n" +
				"Would you like to download and install the new version?",
				"GitMind",
				MessageBoxButton.OKCancel,
				MessageBoxImage.Question))
			{
				return;
			}

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
			catch (Exception ex)
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
			LogViewModel.SetBranches(activeBranches);

			var dialog = new System.Windows.Forms.FolderBrowserDialog();
			dialog.Description = "Select a working folder.";
			dialog.ShowNewFolderButton = false;
			dialog.SelectedPath = Environment.CurrentDirectory;
			if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			{
				return;
			}

			Environment.CurrentDirectory = dialog.SelectedPath;

			await LogViewModel.LoadAsync(owner);

			WorkingFolder.Value = ProgramPaths.TryGetWorkingFolderPath(Environment.CurrentDirectory);
		}
	}
}