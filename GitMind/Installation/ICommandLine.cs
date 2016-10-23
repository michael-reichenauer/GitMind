﻿using System.Collections.Generic;


namespace GitMind.Installation
{
	public interface ICommandLine
	{
		bool IsSilent { get; }
		bool IsInstall { get; }
		bool IsUninstall { get; }
		bool IsRunInstalled { get; }
		bool IsShowDiff { get; }
		string WorkingFolder { get; }
		IReadOnlyList<string> BranchNames { get; }
	}
}