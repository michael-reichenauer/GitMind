using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitMind.ApplicationHandling;
using GitMind.MainWindowViews;
using GitMind.Utils;
using GitMind.Utils.Git;
using Application = System.Windows.Application;


namespace GitMind.RepositoryViews.Open
{
	internal class OpenModelService : IOpenModelService
	{
		private readonly IRecentModelsService recentModelsService;
		private readonly IGitInfoService gitInfoService;
		private readonly IStartInstanceService startInstanceService;
		private readonly WindowOwner owner;


		public OpenModelService(
			IRecentModelsService recentModelsService,
			IGitInfoService gitInfoService,
			IStartInstanceService startInstanceService,
			WindowOwner owner)
		{
			this.owner = owner;
			this.recentModelsService = recentModelsService;
			this.gitInfoService = gitInfoService;
			this.startInstanceService = startInstanceService;
		}


		public async Task OpenModelAsync()
		{
			await Task.Yield();
			if (!TrySelectWorkingFolder(out string folder))
			{
				return;
			}

			startInstanceService.StartInstance(folder);
			Application.Current.Shutdown(0);
		}


		public async Task TryModelAsync(string folder)
		{
			await Task.Yield();

			startInstanceService.StartInstance(folder);
			Application.Current.Shutdown(0);
		}


		public async Task OpenOtherModelAsync(string folder)
		{
			await Task.Yield();

			startInstanceService.StartInstance(folder);
			Application.Current.Shutdown(0);
		}

		

		public async Task OpenModelAsync(IReadOnlyList<string> modelFilePaths)
		{
			// Currently only support one dropped file
			string modelFilePath = modelFilePaths.First();

			await TryModelAsync(modelFilePath);
		}



		private bool TrySelectWorkingFolder(out string folder)
		{
			folder = null;

			while (true)
			{
				var dialog = new FolderBrowserDialog()
				{
					Description = "Select a working folder with a valid git repository.",
					ShowNewFolderButton = false,
					RootFolder = Environment.SpecialFolder.MyComputer
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
			}
		}


		private string GetInitialFolder()
		{
			string lastParentFolder = null;

			string lastUsedFolder = recentModelsService.GetModelPaths().FirstOrDefault();
			if (!string.IsNullOrEmpty(lastUsedFolder))
			{
				string folder = Path.GetDirectoryName(lastUsedFolder);
				if (folder != null && Directory.Exists(folder))
				{
					lastParentFolder = folder;
				}
			}

			Log.Debug($"Initial folder {lastParentFolder}");
			return lastParentFolder;
		}
	}
}