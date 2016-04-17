using System;
using System.IO;
using System.Linq;
using System.Reflection;
using GitMind.Utils;


namespace GitMind.Installation.Private
{
	internal class CommandLine : ICommandLine
	{
		private readonly string[] args;

		public CommandLine()
		{
			args = Environment.GetCommandLineArgs();

			Log.Debug($"Args: '{string.Join("','", args)}'");
		}


		public bool IsSilent => args.Contains("/silent");

		public bool IsInstall => args.Contains("/install") || IsRunningSetupFile;

		public bool IsUninstall => args.Contains("/uninstall");

		public bool IsRunInstalled => args.Contains("/run");

		private bool IsRunningSetupFile => 
			Path.GetFileNameWithoutExtension(
				Assembly.GetEntryAssembly().Location).StartsWith("GitMindSetup");	
	}
}