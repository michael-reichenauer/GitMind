using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitMind.ApplicationHandling;
using GitMind.Common.MessageDialogs;
using GitMind.MainWindowViews;
using GitMind.Utils;
using GitMind.Utils.Git;
using Application = System.Windows.Application;


namespace GitMind.RepositoryViews.Open
{
	internal class OpenRepoService : IOpenRepoService
	{
		private static readonly TimeSpan CloseTime = TimeSpan.FromSeconds(2);

		private readonly IRecentReposService recentReposService;
		private readonly IGitInfoService gitInfoService;
		private readonly IStartInstanceService startInstanceService;
		private readonly IMessage message;
		private readonly WindowOwner owner;


		public OpenRepoService(
			IRecentReposService recentReposService,
			IGitInfoService gitInfoService,
			IStartInstanceService startInstanceService,
			IMessage message,
			WindowOwner owner)
		{
			this.owner = owner;
			this.recentReposService = recentReposService;
			this.gitInfoService = gitInfoService;
			this.startInstanceService = startInstanceService;
			this.message = message;
		}


		public async Task OpenRepoAsync()
		{
			await Task.Yield();
			if (!TrySelectWorkingFolder(out string folder))
			{
				return;
			}
			
			await SwitchToWorkingFolder(folder);
		}


		public async Task TryOpenRepoAsync(string folder)
		{
			await Task.Yield();

			await SwitchToWorkingFolder(folder);
		}


		public async Task OpenOtherRepoAsync(string folder)
		{
			await Task.Yield();

			await SwitchToWorkingFolder(folder);
		}


		public async Task OpenRepoAsync(IReadOnlyList<string> modelFilePaths)
		{
			// Currently only support one dropped file
			string modelFilePath = modelFilePaths.First();

			await TryOpenRepoAsync(modelFilePath);
		}


		private bool TrySelectWorkingFolder(out string folder)
		{
			folder = null;


			var dialog = new FolderBrowserDialog()
			{
				Description = "Select a working folder:",
				ShowNewFolderButton = false,
				//RootFolder = Environment.SpecialFolder.MyComputer
			};

			string lastParentFolder = GetInitialFolder();

			if (lastParentFolder != null)
			{
				dialog.SelectedPath = lastParentFolder;
			}

			if (dialog.ShowDialog(owner.Win32Window) != DialogResult.OK)
			{
				Log.Debug("User canceled selecting a Working folder");
				return false;
			}

			if (!string.IsNullOrWhiteSpace(dialog.SelectedPath) && Directory.Exists(dialog.SelectedPath))
			{
				R<string> rootFolder = gitInfoService.GetWorkingFolderRoot(dialog.SelectedPath);
				
				if (rootFolder.IsOk)
				{
					Log.Debug($"User selected valid working folder: {rootFolder.Value}");
					folder = rootFolder.Value;
					return true;
				}
			}

			Log.Debug($"User selected an invalid working folder: {dialog.SelectedPath}");
			message.ShowInfo($"The selected folder did not contain a valid git repository:\n{dialog.SelectedPath}");
			return false;
		}


		private string GetInitialFolder()
		{
			string lastParentFolder = null;

			string lastUsedFolder = recentReposService.GetRepoPaths().FirstOrDefault();
			if (!string.IsNullOrEmpty(lastUsedFolder))
			{
				if (Directory.Exists(lastUsedFolder))
				{
					return lastUsedFolder;
				}

				string folder = Path.GetDirectoryName(lastUsedFolder);
				if (folder != null && Directory.Exists(folder))
				{
					lastParentFolder = folder;
				}
			}

			Log.Debug($"Initial folder {lastParentFolder}");
			return lastParentFolder;
		}


		private async Task SwitchToWorkingFolder(string workingFolder)
		{
			startInstanceService.OpenOrStartInstance(workingFolder);

			await Task.Delay(CloseTime);
			Application.Current.Shutdown(0);
		}
	}
}