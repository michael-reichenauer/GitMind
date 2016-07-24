using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GitMind.Settings;
using GitMind.Testing;
using GitMind.Utils;


namespace GitMind.Installation.Private
{
	internal class CommandLine : ICommandLine
	{
		private readonly Lazy<string> lazyWorkingFolder;
		private readonly Lazy<IReadOnlyList<string>> lazyBranchNames;
		private readonly string[] args;

		public CommandLine()
		{
			args = Environment.GetCommandLineArgs();
			Version currentVersion = ProgramPaths.GetCurrentVersion();
			Log.Debug($"Version: {currentVersion}, args: '{string.Join("','", args)}'");

			lazyWorkingFolder = new Lazy<string>(GetWorkingFolder);
			lazyBranchNames = new Lazy<IReadOnlyList<string>>(GetBranchNames);
		}


		public bool IsSilent => args.Contains("/silent");

		public bool IsInstall => args.Contains("/install") || IsRunningSetupFile;

		public bool IsUninstall => args.Contains("/uninstall");

		public bool IsRunInstalled => args.Contains("/run");

		private bool IsRunningSetupFile => 
			Path.GetFileNameWithoutExtension(
				Assembly.GetEntryAssembly().Location).StartsWith("GitMindSetup");

		public string WorkingFolder => lazyWorkingFolder.Value;

		public IReadOnlyList<string> BranchNames => lazyBranchNames.Value;


		// Must be able to handle:
		// * Starting app from start menu or pinned (no parameters and unknown current dir)
		// * Starting on command line in some dir (no parameters but known dir)
		// * Starting as right click on folder (parameter "/d:<dir>"
		// * Starting on command line with some parameters (branch names)
		// * Starting with parameters "/test"
		private string GetWorkingFolder()
		{
			string workingFolder = null;

			if (args.Length == 2 && args[1].StartsWith("/d:"))
			{
				// Call from e.g. Windows Explorer folder context menu
				workingFolder = args[1].Substring(3);
			}
			else if (args.Length == 2 && args[1] == "/test" && Directory.Exists(TestRepo.Path))
			{
				workingFolder = TestRepo.Path;
			}
			

			if (workingFolder == null)
			{
				workingFolder = TryGetWorkingFolder();
			}

			Log.Debug($"Current working folder {workingFolder}");
			return workingFolder;
		}


		private IReadOnlyList<string> GetBranchNames()
		{
			List<string> branchNames = new List<string>();
	
			if (!(args.Length == 2 && args[1].StartsWith("/d:"))
				&& !(args.Length == 2 && args[1] == "/test" && Directory.Exists(TestRepo.Path))
				&& args.Length > 1)
			{
				for (int i = 1; i < args.Length; i++)
				{
					branchNames.Add(args[i]);
				}
			}

			return branchNames;
		}


		private static string TryGetWorkingFolder()
		{
			R<string> path = ProgramPaths.GetWorkingFolderPath(Environment.CurrentDirectory);

			if (!path.HasValue)
			{
				string lastUsedFolder = ProgramSettings.TryGetLatestUsedWorkingFolderPath();

				if (!string.IsNullOrWhiteSpace(lastUsedFolder))
				{
					path = ProgramPaths.GetWorkingFolderPath(lastUsedFolder);
				}
			}

			if (path.HasValue)
			{
				return path.Value;
			}

			return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		}
	}
}