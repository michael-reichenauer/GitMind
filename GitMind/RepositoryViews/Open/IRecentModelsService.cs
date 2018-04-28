using System;
using System.Collections.Generic;


namespace GitMind.RepositoryViews.Open
{
	internal interface IRecentModelsService
	{
		event EventHandler Changed;

		void AddModelPaths(string modelFilePath);

		IReadOnlyList<string> GetModelPaths();
	}
}