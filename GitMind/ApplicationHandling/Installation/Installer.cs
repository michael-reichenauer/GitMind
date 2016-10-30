using System;
using System.IO;
using System.Threading;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Common.MessageDialogs;
using GitMind.Utils;
using Microsoft.Win32;


namespace GitMind.ApplicationHandling.Installation
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
		private static readonly string SetupTitle = "GitMind - Setup";



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
			Log.Usage("Install normal.");

			if (!Message.ShowAskOkCancel(
				"Welcome to the GitMind setup.\n\n" +
				" This will:\n" +
				" - Add a GitMind shortcut in the Start Menu.\n" +
				" - Add a 'GitMind' context menu item in Windows File Explorer.\n" +
				" - Make GitMind command available in Command Prompt window.\n\n" +
				"Click OK to install GitMind or Cancel to exit Setup.",
				SetupTitle))
			{
				return;
			}

			if (!EnsureNoOtherInstancesAreRunning())
			{
				return;
			}

			InstallSilent();
			Log.Usage("Installed normal.");

			Message.ShowInfo(
				"Setup has finished installing GitMind.",
				SetupTitle);

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
					if (!Message.ShowAskOkCancel(
						"Please close all instances of GitMind before continue the installation."))
					{
						return false;
					}
				}
			}
		}


		public void InstallSilent()
		{
			Log.Usage("Installing ...");
			string path = CopyFileToProgramFiles();

			AddUninstallSupport(path);
			CreateStartMenuShortcut(path);
			AddToPathVariable(path);
			AddFolderContextMenu();
			Log.Usage("Installed");
		}


		public void UninstallNormal()
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
			Version sourceVersion = ProgramPaths.GetVersion(sourcePath);

			string targetFolder = ProgramPaths.GetProgramFolderPath();

			EnsureDirectoryIsCreated(targetFolder);

			string targetPath = ProgramPaths.GetInstallFilePath();

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
					string oldFilePath = ProgramPaths.GetTempFilePath();
					Log.Debug($"Moving {targetPath} to {oldFilePath}");

					File.Move(targetPath, oldFilePath);
					Log.Debug($"Moved {targetPath} to target {oldFilePath}");
					CopyFile(sourcePath, targetPath);
					WriteInstalledVersion(sourceVersion);
				}
				catch (Exception ex) when (e.IsNotFatal())
				{
					Log.Error($"Failed to copy {sourcePath} to target {targetPath}, {ex}");
					throw;
				}
			}

			return targetPath;
		}


		private void WriteInstalledVersion(Version sourceVersion)
		{
			try
			{
				string path = ProgramPaths.GetVersionFilePath();
				File.WriteAllText(path, sourceVersion.ToString());
				Log.Debug($"Installed {sourceVersion}");
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to write version {e}");
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