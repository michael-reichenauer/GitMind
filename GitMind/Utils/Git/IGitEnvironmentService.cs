﻿using System.Threading;


namespace GitMind.Utils.Git
{
	public interface IGitEnvironmentService
	{
		string GetGitCmdPath();
		//Task<string> TryGetWorkingFolderRootAsync(string path, CancellationToken ct);
		string TryGetGitCorePath();
		string TryGetGitCmdPath();
		string TryGetGitVersion();
		string TryGetWorkingFolderRoot(string path);
	}
}