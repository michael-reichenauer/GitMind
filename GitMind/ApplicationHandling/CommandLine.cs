using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GitMind.Utils;


namespace GitMind.ApplicationHandling
{
	[SingleInstance]
	internal class CommandLine : ICommandLine
	{
		private readonly Lazy<IReadOnlyList<string>> lazyBranchNames = null;
		private readonly string[] args;


		public CommandLine()
		{
			this.args = Environment.GetCommandLineArgs();
			lazyBranchNames = new Lazy<IReadOnlyList<string>>(GetBranchNames);
		}






		public bool IsSilent => args.Contains("/silent");

		public bool IsInstall => args.Contains("/install") || IsSetupFile();

		public bool IsUninstall => args.Contains("/uninstall");

		public bool IsRunInstalled => args.Contains("/run");

		public bool IsShowDiff => args.Length > 1 && args[1] == "diff";

		public bool IsCommit => args.Length > 1 && args[1] == "commit";


		public bool IsCommitCommand(string[] instanceArgs)
		{
			return instanceArgs.Length > 1 && instanceArgs[1] == "commit";
		}


		public bool IsTest => args.Contains("/test");

		public bool HasFolder => args.Any(a => a.StartsWith("/d:")) || IsTest;

		public string Folder => args.FirstOrDefault(a => a.StartsWith("/d:"))?.Substring(3);

		public IReadOnlyList<string> BranchNames => lazyBranchNames.Value;


		private bool IsSetupFile()
		{
			return Path.GetFileNameWithoutExtension(
				Assembly.GetEntryAssembly().Location).StartsWith("GitMindSetup");
		}


		private IReadOnlyList<string> GetBranchNames()
		{
			List<string> branchNames = new List<string>();

			foreach (string arg in args
				.SkipWhile(a => a != "show")
				.TakeWhile(a => !a.StartsWith("/")))
			{
				branchNames.Add(arg);
			}

			return branchNames;
		}
	}
}