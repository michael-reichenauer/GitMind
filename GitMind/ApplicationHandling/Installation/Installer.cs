using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GitMind.ApplicationHandling.Installation.StarMenuHandling;
using GitMind.Common.MessageDialogs;
using GitMind.Common.ProgressHandling;
using GitMind.Common.Tracking;
using GitMind.Utils;
using GitMind.Utils.Git;
using Microsoft.Win32;


namespace GitMind.ApplicationHandling.Installation
{
	internal class Installer : IInstaller
	{
		private readonly ICommandLine commandLine;
		public static readonly string ProductGuid = "0000278d-5c40-4973-aad9-1c33196fd1a2";

		private static readonly string UninstallSubKey =
			$"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{{{ProductGuid}}}_is1";
		private static readonly string UninstallRegKey = "HKEY_CURRENT_USER\\" + UninstallSubKey;
		private static readonly string subFolderContextMenuPath =
			"Software\\Classes\\Folder\\shell\\gitmind";
		private static readonly string subDirectoryBackgroundContextMenuPath =
			"Software\\Classes\\Directory\\Background\\shell\\gitmind";
		private static readonly string folderContextMenuPath =
			"HKEY_CURRENT_USER\\" + subFolderContextMenuPath;
		private static readonly string directoryContextMenuPath =
			"HKEY_CURRENT_USER\\" + subDirectoryBackgroundContextMenuPath;
		private static readonly string folderCommandContextMenuPath =
			folderContextMenuPath + "\\command";
		private static readonly string directoryCommandContextMenuPath =
			directoryContextMenuPath + "\\command";
		private static readonly string SetupTitle = "GitMind - Setup";

		private static readonly string ProgramShortcutFileName = ProgramInfo.ProgramName + ".lnk";



		private readonly ICmd cmd;
		private readonly IGitEnvironmentService gitEnvironmentService;
		private readonly IProgressService progressService;


		public Installer(
			ICommandLine commandLine,
			ICmd cmd,
			IGitEnvironmentService gitEnvironmentService,
			IProgressService progressService)
		{
			this.commandLine = commandLine;
			this.cmd = cmd;
			this.gitEnvironmentService = gitEnvironmentService;
			this.progressService = progressService;
		}


		public bool InstallOrUninstall()
		{
			if (commandLine.IsInstall && !commandLine.IsSilent)
			{
				Track.Request("Install-Normal");
				InstallNormal();

				return false;
			}
			else if (commandLine.IsInstall && commandLine.IsSilent)
			{
				Track.Request("Install-Silent");
				Task.Run(async () => await InstallSilentAsync(s => Log.Info(s)).ConfigureAwait(false)).Wait();

				if (commandLine.IsRunInstalled)
				{
					StartInstalled();
				}

				return false;
			}
			else if (commandLine.IsUninstall && !commandLine.IsSilent)
			{
				Track.Event("Uninstall-Normal");
				UninstallNormal();

				return false;
			}
			else if (commandLine.IsUninstall && commandLine.IsSilent)
			{
				Track.Request("Uninstall-Silent");
				UninstallSilent();

				return false;
			}

			return true;
		}


		private void InstallNormal()
		{
			Log.Usage("Install normal.");

			InstallDialog dialog = null;

			bool isCanceled = false;
			async Task InstallActionAsync()
			{
				if (!EnsureNoOtherInstancesAreRunning(dialog))
				{
					isCanceled = true;
					return;
				}

				dialog.Message = "";
				dialog.IsButtonsVisible = false;

				Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
				using (Progress progress = progressService.ShowDialog("", dialog))
				{
					await InstallSilentAsync(text => dispatcher.Invoke(() => progress.SetText(text)));
				}

				Message.ShowInfo(
					"Setup has finished installing GitMind.",
					SetupTitle,
					dialog);
				Log.Usage("Installed normal.");
			}

			dialog = new InstallDialog(
				null,
				"Welcome to the GitMind setup.\n\n" +
				" This will:\n" +
				" - Add a GitMind shortcut in the Start Menu.\n" +
				" - Add a 'GitMind' context menu item in Windows File Explorer.\n" +
				" - Make GitMind command available in Command Prompt window.\n\n" +
				"Click Install to install GitMind or Cancel to exit Setup.",
				SetupTitle,
				(InstallActionAsync)
				);

			bool? showDialog = dialog.ShowDialog();
			Log.Debug($"Dialog result: {showDialog}");

			if (showDialog != true)
			{
				Log.Usage("Install canceled.");
				Log.Warn("Dialog canceled");
				return;
			}

			if (isCanceled)
			{
				Log.Usage("Install canceled.");
				Log.Warn("Is canceled");
				return;
			}

			StartInstalled();
		}


		private void StartInstalled()
		{
			string targetPath = ProgramInfo.GetInstallFilePath();
			cmd.Start(targetPath, "/run /d:Open");
		}


		private static bool EnsureNoOtherInstancesAreRunning(Window owner = null)
		{
			while (true)
			{
				bool created = false;
				using (new Mutex(true, ProductGuid, out created))
				{
					if (created)
					{
						return true;
					}

					Log.Debug("GitMind instance is already running, needs to be closed.");
					if (!Message.ShowAskOkCancel(
						"Please close all instances of GitMind before continue the installation.",
						"GitMind",
						owner))
					{
						return false;
					}

					Thread.Sleep(1000);
				}
			}
		}


		private async Task<R> InstallSilentAsync(Action<string> progress)
		{
			Log.Usage("Installing ...");
			progress("Installing GitMind ...");
			string path = CopyFileToProgramFiles();
			await Task.Yield();
			AddUninstallSupport(path);
			await Task.Yield();
			CreateStartMenuShortcut(path);
			await Task.Yield();
			AddToPathVariable(path);
			await Task.Yield();
			AddFolderContextMenu();
			await Task.Yield();

			progress.Invoke("Downloading git ...");
			R gitResult = await gitEnvironmentService.InstallGitAsync(progress);
			Log.Usage("Installed");

			if (gitResult.IsFaulted)
			{
				Track.Error($"Failed to install git {gitResult}");
				return gitResult;
			}

			return R.Ok;
		}


		private void UninstallNormal()
		{
			Log.Usage("Uninstall normal");
			if (IsInstalledInstance())
			{
				// The running instance is the file, which should be deleted and would block deletion,
				// Copy the file to temp and run uninstallation from that file.
				string tempPath = CopyFileToTemp();
				Log.Debug("Start uninstaller in tmp folder");
				cmd.Start(tempPath, "/uninstall");
				return;
			}

			if (!Message.ShowAskOkCancel(
				"Do you want to uninstall GitMind?"))
			{
				return;
			}

			if (!EnsureNoOtherInstancesAreRunning())
			{
				return;
			}

			UninstallSilent();
			Log.Usage("Uninstalled normal");
			Message.ShowInfo("Uninstallation of GitMind is completed.");
		}


		private void UninstallSilent()
		{
			Log.Debug("Uninstalling...");
			DeleteProgramFilesFolder();
			DeleteProgramDataFolder();
			DeleteStartMenuShortcut();
			DeleteInPathVariable();
			DeleteFolderContextMenu();
			DeleteUninstallSupport();
			Log.Debug("Uninstalled");
		}

		private string CopyFileToProgramFiles()
		{
			string sourcePath = ProgramInfo.GetCurrentInstancePath();
			Version sourceVersion = ProgramInfo.GetVersion(sourcePath);

			string targetFolder = ProgramInfo.GetProgramFolderPath();

			EnsureDirectoryIsCreated(targetFolder);

			string targetPath = ProgramInfo.GetInstallFilePath();

			try
			{
				if (sourcePath != targetPath)
				{
					CopyFile(sourcePath, targetPath);
					WriteInstalledVersion(sourceVersion);
				}
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Debug($"Failed to copy {sourcePath} to target {targetPath} {e.Message}");
				try
				{
					string oldFilePath = ProgramInfo.GetTempFilePath();
					Log.Debug($"Moving {targetPath} to {oldFilePath}");

					File.Move(targetPath, oldFilePath);
					Log.Debug($"Moved {targetPath} to target {oldFilePath}");
					CopyFile(sourcePath, targetPath);
					WriteInstalledVersion(sourceVersion);
				}
				catch (Exception ex) when (e.IsNotFatal())
				{
					Log.Exception(ex, $"Failed to copy {sourcePath} to target {targetPath}");
					throw;
				}
			}

			return targetPath;
		}


		private void WriteInstalledVersion(Version sourceVersion)
		{
			try
			{
				string path = ProgramInfo.GetVersionFilePath();
				File.WriteAllText(path, sourceVersion.ToString());
				Log.Debug($"Installed {sourceVersion}");
			}
			catch (Exception e)
			{
				Log.Exception(e, "Failed to write version");
			}
		}


		private static void CopyFile(string sourcePath, string targetPath)
		{
			// Not using File.Copy, to avoid copying possible "downloaded from internet flag"
			byte[] fileData = File.ReadAllBytes(sourcePath);
			File.WriteAllBytes(targetPath, fileData);
			Log.Debug($"Copied {sourcePath} to target {targetPath}");
		}


		private static void EnsureDirectoryIsCreated(string targetFolder)
		{
			if (!Directory.Exists(targetFolder))
			{
				Directory.CreateDirectory(targetFolder);
			}
		}


		private static void DeleteProgramFilesFolder()
		{
			Thread.Sleep(300);
			string folderPath = ProgramInfo.GetProgramFolderPath();

			for (int i = 0; i < 5; i++)
			{
				try
				{
					if (Directory.Exists(folderPath))
					{
						Directory.Delete(folderPath, true);
					}
					else
					{
						return;
					}
				}
				catch (Exception e) when (e.IsNotFatal())
				{
					Log.Debug($"Failed to delete {folderPath}");
					Thread.Sleep(1000);
				}
			}
		}


		private static void DeleteProgramDataFolder()
		{
			string programDataFolderPath = ProgramInfo.GetProgramDataFolderPath();

			if (Directory.Exists(programDataFolderPath))
			{
				Directory.Delete(programDataFolderPath, true);
			}
		}



		private static void CreateStartMenuShortcut(string pathToExe)
		{
			string shortcutLocation = GetStartMenuShortcutPath();

			StartMenuWrapper.CreateStartMenuShortCut(
				shortcutLocation,
				pathToExe,
				"",
				pathToExe, 
				ProgramInfo.ProgramName);
		}


		private static void DeleteStartMenuShortcut()
		{
			string shortcutLocation = GetStartMenuShortcutPath();
			File.Delete(shortcutLocation);
		}


		private static void AddToPathVariable(string path)
		{
			string folderPath = Path.GetDirectoryName(path);

			string keyName = @"Environment\";
			string pathsVariables = (string)Registry.CurrentUser.OpenSubKey(keyName)
				.GetValue("PATH", "", RegistryValueOptions.DoNotExpandEnvironmentNames);

			pathsVariables = pathsVariables.Trim();

			if (!pathsVariables.Contains(folderPath))
			{
				if (!string.IsNullOrEmpty(pathsVariables) && !pathsVariables.EndsWith(";"))
				{
					pathsVariables += ";";
				}

				pathsVariables += folderPath;
				Environment.SetEnvironmentVariable(
					"PATH", pathsVariables, EnvironmentVariableTarget.User);
			}
		}


		private static void DeleteInPathVariable()
		{
			string programFilesFolderPath = ProgramInfo.GetProgramFolderPath();

			string keyName = @"Environment\";
			string pathsVariables = (string)Registry.CurrentUser.OpenSubKey(keyName)
				.GetValue("PATH", "", RegistryValueOptions.DoNotExpandEnvironmentNames);

			string pathPart = programFilesFolderPath;
			if (pathsVariables.Contains(pathPart))
			{
				pathsVariables = pathsVariables.Replace(pathPart, "");
				pathsVariables = pathsVariables.Replace(";;", ";");
				pathsVariables = pathsVariables.Trim(";".ToCharArray());

				Registry.SetValue("HKEY_CURRENT_USER\\" + keyName, "PATH", pathsVariables);
			}
		}


		private static void AddUninstallSupport(string path)
		{
			string version = ProgramInfo.GetVersion(path).ToString();

			Registry.SetValue(UninstallRegKey, "DisplayName", ProgramInfo.ProgramName);
			Registry.SetValue(UninstallRegKey, "DisplayIcon", path);
			Registry.SetValue(UninstallRegKey, "Publisher", "Michael Reichenauer");
			Registry.SetValue(UninstallRegKey, "DisplayVersion", version);
			Registry.SetValue(UninstallRegKey, "UninstallString", path + " /uninstall");
			Registry.SetValue(UninstallRegKey, "EstimatedSize", 1000);
		}



		private static void DeleteUninstallSupport()
		{
			try
			{
				Registry.CurrentUser.DeleteSubKeyTree(UninstallSubKey);
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Exception(e, "Failed to delete uninstall support");
			}
		}


		private static void AddFolderContextMenu()
		{
			string programFilePath = ProgramInfo.GetInstallFilePath();

			Registry.SetValue(folderContextMenuPath, "", ProgramInfo.ProgramName);
			Registry.SetValue(folderContextMenuPath, "Icon", programFilePath);
			Registry.SetValue(folderCommandContextMenuPath, "", "\"" + programFilePath + "\" \"/d:%1\"");

			Registry.SetValue(directoryContextMenuPath, "", ProgramInfo.ProgramName);
			Registry.SetValue(directoryContextMenuPath, "Icon", programFilePath);
			Registry.SetValue(
				directoryCommandContextMenuPath, "", "\"" + programFilePath + "\" \"/d:%V\"");
		}


		private static void DeleteFolderContextMenu()
		{
			try
			{
				Registry.CurrentUser.DeleteSubKeyTree(subFolderContextMenuPath);
				Registry.CurrentUser.DeleteSubKeyTree(subDirectoryBackgroundContextMenuPath);
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Exception(e, "Failed to delete folder context menu");
			}
		}


		private static bool IsInstalledInstance()
		{
			string folderPath = Path.GetDirectoryName(ProgramInfo.GetCurrentInstancePath());
			string programFolderGitMind = ProgramInfo.GetProgramFolderPath();

			return folderPath == programFolderGitMind;
		}


		private static string CopyFileToTemp()
		{
			string sourcePath = ProgramInfo.GetCurrentInstancePath();
			string targetPath = Path.Combine(Path.GetTempPath(), ProgramInfo.ProgramFileName);
			File.Copy(sourcePath, targetPath, true);

			return targetPath;
		}


		public static string GetStartMenuShortcutPath()
		{
			string commonStartMenuPath = Environment.GetFolderPath(
				Environment.SpecialFolder.StartMenu);
			string startMenuPath = Path.Combine(commonStartMenuPath, "Programs");

			return Path.Combine(startMenuPath, ProgramShortcutFileName);
		}
	}
}