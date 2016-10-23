using System.Collections.Generic;


namespace GitMind.Installation
{
	public interface ICommandLine
	{
		bool IsSilent { get; }
		bool IsInstall { get; }
		bool IsUninstall { get; }
		bool IsRunInstalled { get; }
		bool IsShowDiff { get; }
		bool IsTest { get; }
		bool HasFolder { get; }
		string Folder { get; }
		IReadOnlyList<string> BranchNames { get; }
		bool IsCommit { get; }
	}
}