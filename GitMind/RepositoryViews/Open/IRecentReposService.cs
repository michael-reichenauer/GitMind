using System;
using System.Collections.Generic;


namespace GitMind.RepositoryViews.Open
{
	internal interface IRecentReposService
	{
		event EventHandler Changed;

		void AddRepoPaths(string modelFilePath);

		IReadOnlyList<string> GetRepoPaths();
	}
}