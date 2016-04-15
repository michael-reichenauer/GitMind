using System;
using System.IO;
using System.Reflection;


namespace GitMind.Installation.Private
{
	internal class CommandLine : ICommandLine
	{
		private readonly string[] args;

		public CommandLine()
		{
			args = Environment.GetCommandLineArgs();
		}

		public bool IsNormalInstallation()
		{
			return
				(args.Length == 1 && IsRunningSetupFile())
				|| (args.Length == 2 && args[1] == "/install");
		}


		public bool IsSilentInstallation()
		{
			return
				(args.Length == 3 && args[1] == "/install" && args[2] == "/silent")
				|| (args.Length == 2 && IsRunningSetupFile() && args[1] == "/silent");
		}


		private static bool IsRunningSetupFile()
		{
			return Path.GetFileNameWithoutExtension(
				Assembly.GetEntryAssembly().Location).StartsWith("GitMindSetup");
		}


		public bool IsNormalUninstallation()
		{
			return args.Length == 2 && args[1] == "/uninstall";
		}


		public bool IsSilentUninstallation()
		{
			return args.Length == 3 && args[1] == "/uninstall" && args[2] == "/silent";
		}
	}
}