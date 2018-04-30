using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GitMind.ApplicationHandling;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
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
		private readonly IGitRepoService gitRepoService;
		private readonly IStartInstanceService startInstanceService;
		private readonly IMessage message;
		private readonly IProgressService progressService;
		private readonly WindowOwner owner;


		public OpenRepoService(
			IRecentReposService recentReposService,
			IGitInfoService gitInfoService,
			IGitRepoService gitRepoService,
			IStartInstanceService startInstanceService,
			IMessage message,
			IProgressService progressService,
			WindowOwner owner)
		{
			this.owner = owner;
			this.recentReposService = recentReposService;
			this.gitInfoService = gitInfoService;
			this.gitRepoService = gitRepoService;
			this.startInstanceService = startInstanceService;
			this.message = message;
			this.progressService = progressService;
		}


		public async Task OpenRepoAsync()
		{
			if (!TrySelectWorkingFolder(out string folder))
			{
				return;
			}
			
			await SwitchToWorkingFolder(folder);
		}


		public async Task TryOpenRepoAsync(string folder) => await SwitchToWorkingFolder(folder);


		public async Task OpenOtherRepoAsync(string folder) => await SwitchToWorkingFolder(folder);


		public async Task OpenRepoAsync(IReadOnlyList<string> modelFilePaths)
		{
			// Currently only support one dropped file
			string modelFilePath = modelFilePaths.First();

			await TryOpenRepoAsync(modelFilePath);
		}


		public async Task CloneRepoAsync()
		{
			CloneDialog cloneDialog = new CloneDialog(owner);
			IReadOnlyList<string> resentUris = recentReposService.GetCloneUriPaths();
			resentUris.ForEach(uri => cloneDialog.AddUri(uri));

			string folder = GetInitialCloneFolder() ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

			cloneDialog.SetInitialFolder(folder);

			if (cloneDialog.ShowDialog() == true)
			{
				await CloneAsync(cloneDialog.UriText, cloneDialog.FolderText, CancellationToken.None);
			}
		}


		public async Task InitRepoAsync()
		{
			FolderBrowserDialog dialog = new FolderBrowserDialog()
			{
				Description = "Select a folder to initialize as a Git repository:",
				ShowNewFolderButton = true,
			};

			string lastParentFolder = GetInitialOpenFolder();

			if (lastParentFolder != null)
			{
				dialog.SelectedPath = lastParentFolder;
			}

			if (dialog.ShowDialog(owner.Win32Window) != DialogResult.OK)
			{
				Log.Debug("User canceled selecting a Working folder");
				return;
			}
			
			string path = dialog.SelectedPath;

			R<string> rootFolder = gitInfoService.GetWorkingFolderRoot(path);

			if (rootFolder.IsOk)
			{
				if (rootFolder.Value.SameIc(path))
				{
					message.ShowWarning($"The selected folder is already a git repository:\n{path}");
				}
				else
				{
					message.ShowWarning($"The selected folder is a sub-folder of a git repository at:\n{rootFolder.Value}");
				}
			
				return;
			}

			R result = await gitRepoService.InitAsync(path, CancellationToken.None);
			if (result.IsFaulted)
			{
				message.ShowError($"Error: {result.Error}");
				return;
			}

			await SwitchToWorkingFolder(path);
		}


		private async Task CloneAsync(string uri, string folder, CancellationToken ct)
		{
			void SetProgress(Progress progress, string text)
			{
				progress.SetText($"Cloning {uri} \n{text}");
			}

			try
			{
				if (!Directory.Exists(folder))
				{
					Directory.CreateDirectory(folder);
				}

				using (Progress progress = progressService.ShowDialog($"Cloning {uri} into:\n{folder} ..."))
				{
					R result = await gitRepoService.CloneAsync(uri, folder, text => SetProgress(progress, text), ct);
					if (result.IsFaulted)
					{
						message.ShowError($"Error: {result.Error}");
						return;
					}

					recentReposService.AddCloneUri(uri);
					await SwitchToWorkingFolder(folder);
				}
			}
			catch (Exception e)
			{
				message.ShowError($"Failed to clone {uri}\ninto:{folder}\n\nError: {e.Message}");
			}
		}


		private bool TrySelectWorkingFolder(out string folder)
		{
			folder = null;

			FolderBrowserDialog dialog = new FolderBrowserDialog()
			{
				Description = "Select a working folder:",
				ShowNewFolderButton = false,
			};

			string lastParentFolder = GetInitialOpenFolder();

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


		private string GetInitialOpenFolder()
		{
			string lastParentFolder = null;

			string lastUsedFolder = recentReposService.GetWorkFolderPaths().FirstOrDefault();
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


		private string GetInitialCloneFolder()
		{
			string lastParentFolder = null;

			string lastUsedFolder = recentReposService.GetWorkFolderPaths().FirstOrDefault();
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


		private async Task SwitchToWorkingFolder(string workingFolder)
		{

			if (!(Directory.Exists(workingFolder) && 
				gitInfoService.GetWorkingFolderRoot(workingFolder).IsOk))
			{
				message.ShowWarning($"Path is not a valid working folder: {workingFolder}");
				recentReposService.RemoveWorkFolderPath(workingFolder);
				workingFolder = "Open";
			}

			startInstanceService.OpenOrStartInstance(workingFolder);

			await Task.Delay(CloseTime);
			Application.Current.Shutdown(0);
		}
	}
}