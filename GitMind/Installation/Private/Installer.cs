using System;
using System.IO;
using System.Threading;
using System.Windows;
using GitMind.Settings;
using GitMind.Utils;
using Microsoft.Win32;


namespace GitMind.Installation.Private
{
	internal class Installer : IInstaller
	{
		private readonly ICmd cmd;

		private static readonly string UninstallSubKey =
			$"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{{{ProgramPaths.ProductGuid}}}_is1";
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
		private readonly string SetupTitle = "GitMind - Setup";


		public Installer()
			: this(new Cmd())
		{
		}


		public Installer(ICmd cmd)
		{
			this.cmd = cmd;
		}


		public void InstallNormal()
		{
			Log.Debug("Install normal.");
			if (MessageBoxResult.OK != MessageBox.Show(
				Application.Current.MainWindow,
				"Welcome to the GitMind setup.\n\n" +
				" This will:\n" +
				" - Add a GitMind shortcut in the Start Menu.\n" +
				" - Add a 'GitMind' context menu item in Windows File Explorer.\n" +
				" - Make GitMind command available in Command Prompt window.\n\n" +
				"Click OK to install GitMind or Cancel to exit Setup.",
				SetupTitle,
				MessageBoxButton.OKCancel,
				MessageBoxImage.Information))
			{
				return;
			}

			if (!EnsureNoOtherInstancesAreRunning())
			{
				return;
			}

			InstallSilent();

			MessageBox.Show(
				Application.Current.MainWindow,
				"Setup has finished installing GitMind.",
				SetupTitle,
				MessageBoxButton.OK,
				MessageBoxImage.Information);

			StartInstalled();
		}



		public void StartInstalled()
		{
			string targetPath = ProgramPaths.GetInstallFilePath();
			cmd.Start(targetPath, "");
		}


		private static bool EnsureNoOtherInstancesAreRunning()
		{
			while (true)
			{
				bool created = false;
				using (new Mutex(true, ProgramPaths.ProductGuid, out created))
				{
					if (created)
					{
						return true;
					}

					Log.Debug("GitMind instance is already running, needs to be closed.");
					if (MessageBoxResult.OK != MessageBox.Show(
						"Please close all instances of GitMind before continue the installation.",
						ProgramPaths.ProgramName,
						MessageBoxButton.OKCancel,
						MessageBoxImage.Warning))
					{
						return false;
					}
				}
			}
		}


		public void InstallSilent()
		{
			Log.Debug("Installing ...");
			string path = CopyFileToProgramFiles();

			AddUninstallSupport(path);
			CreateStartMenuShortcut(path);
			AddToPathVariable(path);
			AddFolderContextMenu();
			Log.Debug("Installed");
		}


		public void UninstallNormal()
		{
			Log.Debug("Uninstall normal");
			if (IsInstalledInstance())
			{
				// The running instance is the file, which should be deleted and would block deletion,
				// Copy the file to temp and run uninstallation from that file.
				string tempPath = CopyFileToTemp();
				Log.Debug("Start uninstaller in tmp folder");
				cmd.Start(tempPath, "/uninstall");
				return;
			}

			if (MessageBoxResult.OK != MessageBox.Show(
				"Do you want to uninstall GitMind?",
				ProgramPaths.ProgramName,
				MessageBoxButton.OKCancel,
				MessageBoxImage.Question))
			{
				return;
			}

			if (!EnsureNoOtherInstancesAreRunning())
			{
				return;
			}

			UninstallSilent();

			MessageBox.Show(
				"Uninstallation of GitMind is completed.",
				ProgramPaths.ProgramName,
				MessageBoxButton.OK,
				MessageBoxImage.Information);
		}


		public void UninstallSilent()
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
			string sourcePath = ProgramPaths.GetCurrentInstancePath();

			string targetFolder = ProgramPaths.GetProgramFolderPath();

			EnsureDirectoryIsCreated(targetFolder);

			string targetPath = ProgramPaths.GetInstallFilePath();

			try
			{
				if (sourcePath != targetPath)
				{
					CopyFile(sourcePath, targetPath);
				}
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Debug($"Failed to copy {sourcePath} to target {targetPath} {e.Message}");
				try
				{
					string oldFilePath = targetPath + "_old";
					Log.Debug($"Moving {targetPath} to {oldFilePath}");
					if (File.Exists(oldFilePath))
					{
						try
						{
							File.Delete(oldFilePath);
							File.Move(targetPath, oldFilePath);
							Log.Debug($"Moved {targetPath} to target {oldFilePath}");
							CopyFile(sourcePath, targetPath);
						}
						catch (Exception) when (e.IsNotFatal())
						{
							Log.Debug($"Failed to move {targetPath} to target {oldFilePath} {e.Message}");
							CopyFile(sourcePath, targetPath);
						}
					}
					else
					{
						File.Move(targetPath, oldFilePath);
						Log.Debug($"Moved {targetPath} to target {oldFilePath}");
						CopyFile(sourcePath, targetPath);
					}
				}
				catch (Exception ex) when (e.IsNotFatal())
				{
					Log.Error($"Failed to copy {sourcePath} to target {targetPath}, {ex}");
					throw;
				}
			}

			return targetPath;
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
			string folderPath = ProgramPaths.GetProgramFolderPath();

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
			string programDataFolderPath = ProgramPaths.GetProgramDataFolderPath();

			if (Directory.Exists(programDataFolderPath))
			{
				Directory.Delete(programDataFolderPath, true);
			}
		}



		private static void CreateStartMenuShortcut(string pathToExe)
		{
			string shortcutLocation = ProgramPaths.GetStartMenuShortcutPath();

			IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
			IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)
				shell.CreateShortcut(shortcutLocation);

			shortcut.Description = ProgramPaths.ProgramName;
			shortcut.Arguments = "";

			shortcut.IconLocation = pathToExe;
			shortcut.TargetPath = pathToExe;
			shortcut.Save();
		}


		private static void DeleteStartMenuShortcut()
		{
			string shortcutLocation = ProgramPaths.GetStartMenuShortcutPath();
			File.Delete(shortcutLocation);
		}


		private static void AddToPathVariable(string path)
		{
			string folderPath = Path.GetDirectoryName(path);

			string keyName = @"Environment\";
			string pathsVariables = (string)Registry.CurrentUser.OpenSubKey(keyName)
				.GetValue("PATH", "", RegistryValueOptions.DoNotExpandEnvironmentNames);

			if (!pathsVariables.Contains(folderPath))
			{
				if (!pathsVariables.EndsWith(";"))
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
			string programFilesFolderPath = ProgramPaths.GetProgramFolderPath();

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
			string version = ProgramPaths.GetVersion(path).ToString();

			Registry.SetValue(UninstallRegKey, "DisplayName", ProgramPaths.ProgramName);
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
				Log.Warn($"Failed to delete uninstall support {e}");
			}
		}



		private static void AddFolderContextMenu()
		{
			string programFilePath = ProgramPaths.GetInstallFilePath();

			Registry.SetValue(folderContextMenuPath, "", ProgramPaths.ProgramName);
			Registry.SetValue(folderContextMenuPath, "Icon", programFilePath);
			Registry.SetValue(folderCommandContextMenuPath, "", "\"" + programFilePath + "\" \"/d:%1\"");

			Registry.SetValue(directoryContextMenuPath, "", ProgramPaths.ProgramName);
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
				Log.Warn($"Failed to delete folder context menu {e}");
			}

		}


		private static bool IsInstalledInstance()
		{
			string folderPath = Path.GetDirectoryName(ProgramPaths.GetCurrentInstancePath());
			string programFolderGitMind = ProgramPaths.GetProgramFolderPath();

			return folderPath == programFolderGitMind;
		}


		private static string CopyFileToTemp()
		{
			string sourcePath = ProgramPaths.GetCurrentInstancePath();
			string targetPath = Path.Combine(Path.GetTempPath(), ProgramPaths.ProgramFileName);
			File.Copy(sourcePath, targetPath, true);

			return targetPath;
		}
	}
}