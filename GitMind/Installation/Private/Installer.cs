using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using GitMind.Settings;
using GitMind.Utils;
using Microsoft.Win32;


namespace GitMind.Installation.Private
{
	internal class Installer : IInstaller
	{
		private static readonly string UninstallSubKey =
			$"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{{{ProgramPaths.ProductGuid}}}_is1";
		private static readonly string UninstallRegKey = "HKEY_CURRENT_USER\\" + UninstallSubKey;
		private static readonly string subFolderContextMenuPath =
			"Software\\Classes\\Folder\\shell\\gitmind";
		private static readonly string folderContextMenuPath =
			"HKEY_CURRENT_USER\\" + subFolderContextMenuPath;
		private static readonly string folderCommandContextMenuPath =
			folderContextMenuPath + "\\command";
		private readonly string SetupTitle = "GitMind - Setup";



		public void StartInstalledInstance()
		{
			string targetPath = ProgramPaths.GetInstalledFilePath();

			ProcessStartInfo info = new ProcessStartInfo(targetPath);
			info.Arguments = "";
			info.UseShellExecute = true;
			try
			{
				Log.Error($"Starting installed path, {targetPath}");

				Process.Start(info);
			}
			catch (Exception e)
			{
				Log.Error($"Failed to start installed instance, {e}");
				throw;
			}
		}

		public void InstallNormal()
		{
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

			StartInstalledInstance();
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
			try
			{
				string path = CopyFileToProgramFiles();
				AddUninstallSupport(path);
				CreateStartMenuShortcut(path);
				AddToPathVariable(path);
				AddFolderContextMenu();
			}
			catch (Exception e)
			{
				Log.Error($"Failed to install {e}");
				throw;
			}
		}


		public void UninstallNormal()
		{
			if (IsInstalledInstance())
			{
				// The running instance is the file, which should be deleted and would block deletion,
				// Copy the file to temp and run uninstallation from that file.
				string tempPath = CopyFileToTemp();
				StartUnistallInTempFile(tempPath);
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
			DeleteProgramFilesFolder();
			DeleteProgramDataFolder();
			DeleteStartMenuShortcut();
			DeleteInPathVariable();
			DeleteFolderContexteMenu();
			DeleteUninstallSupport();
		}



		private void StartUnistallInTempFile(string path)
		{
			ProcessStartInfo info = new ProcessStartInfo(path);
			info.Arguments = "/uninstall";
			info.UseShellExecute = true;
			try
			{
				Log.Error($"Starting in temp path, {path}");

				Process.Start(info);
			}
			catch (Exception e)
			{
				Log.Error($"Failed to start temp path, {e}");
				throw;
			}
		}


		private string CopyFileToProgramFiles()
		{
			string sourcePath = ProgramPaths.GetCurrentInstancePath();

			string targetFolder = ProgramPaths.GetProgramFolderPath();

			if (!Directory.Exists(targetFolder))
			{
				Directory.CreateDirectory(targetFolder);
			}

			string targetPath = ProgramPaths.GetInstalledFilePath();


			try
			{
				if (sourcePath != targetPath)
				{
					File.Copy(sourcePath, targetPath, true);
				}
			}
			catch (Exception e)
			{
				Log.Debug($"Failed to copy {sourcePath} to target {targetPath}, trying to move target, {e}");
				try
				{
					string oldFilePath = targetPath + "_old";
					if (File.Exists(oldFilePath))
					{
						try
						{
							File.Delete(oldFilePath);
							File.Move(targetPath, oldFilePath);
							File.Copy(sourcePath, targetPath, true);
						}
						catch (Exception)
						{
							File.Copy(sourcePath, targetPath);
						}
					}
					else
					{
						File.Move(targetPath, oldFilePath);
						File.Copy(sourcePath, targetPath, true);
					}
				}
				catch (Exception ex)
				{
					Log.Error($"Failed to copy {sourcePath} to target {targetPath}, {ex}");
					throw;
				}
			}

			return targetPath;
		}


		private void DeleteProgramFilesFolder()
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
				catch (Exception)
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


		private void DeleteInPathVariable()
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
			Registry.SetValue(UninstallRegKey, "DisplayVersion", version);
			Registry.SetValue(UninstallRegKey, "UninstallString", path + " /uninstall");
		}



		private static void DeleteUninstallSupport()
		{
			try
			{
				Registry.CurrentUser.DeleteSubKeyTree(UninstallSubKey);
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to delete uninstall support {e}");
			}
		}



		private void AddFolderContextMenu()
		{
			string programFilePath = ProgramPaths.GetInstalledFilePath();

			Registry.SetValue(folderContextMenuPath, "", ProgramPaths.ProgramName);
			Registry.SetValue(folderCommandContextMenuPath, "", "\"" + programFilePath + "\" \"/d:%1\"");
		}


		private void DeleteFolderContexteMenu()
		{
			try
			{
				Registry.CurrentUser.DeleteSubKeyTree(subFolderContextMenuPath);
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to delete folder context menu {e}");
			}

		}


		private bool IsInstalledInstance()
		{
			string folderPath = Path.GetDirectoryName(ProgramPaths.GetCurrentInstancePath());
			string programFolderGitMind = ProgramPaths.GetProgramFolderPath();

			Log.Warn($"Path1 {folderPath}");
			Log.Warn($"Path2 {programFolderGitMind}");
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