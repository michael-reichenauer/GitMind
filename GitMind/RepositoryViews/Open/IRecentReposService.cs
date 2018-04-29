using System;
using System.Collections.Generic;


namespace GitMind.RepositoryViews.Open
{
	internal interface IRecentReposService
	{
		event EventHandler Changed;

		void AddWorkFolderPath(string folderPath);
		void RemoveWorkFolderPath(string workingFolder);

		void AddCloneUri(string uri);

		IReadOnlyList<string> GetWorkFolderPaths();

		IReadOnlyList<string> GetCloneUriPaths();
	}
}