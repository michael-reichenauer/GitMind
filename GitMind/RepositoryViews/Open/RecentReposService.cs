using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Utils;


namespace GitMind.RepositoryViews.Open
{
	[SingleInstance]
	internal class RecentReposService : IRecentReposService
	{
		private readonly IJumpListService jumpListService;


		public RecentReposService(
			IJumpListService jumpListService)
		{
			this.jumpListService = jumpListService;
		}


		public event EventHandler Changed;
		
		public void AddWorkFolderPath(string folderPath)
		{
			AddToResentPathInProgramSettings(folderPath);

			jumpListService.AddPath(folderPath);
			Changed?.Invoke(this, EventArgs.Empty);
		}


		public IReadOnlyList<string> GetWorkFolderPaths() => 
			Settings.Get<ProgramSettings>().ResentWorkFolderPaths.ToList();


		public IReadOnlyList<string> GetCloneUriPaths() => 
			Settings.Get<ProgramSettings>().ResentCloneUriPaths.ToList();


		public void AddCloneUri(string uri)
		{
			List<string> resentPaths = Settings.Get<ProgramSettings>().ResentCloneUriPaths;

			AddResent(uri, resentPaths);

			Settings.Edit<ProgramSettings>(s => { s.ResentCloneUriPaths = resentPaths; });
		}


		private static void AddToResentPathInProgramSettings(string folderPath)
		{
			List<string> resentPaths = Settings.Get<ProgramSettings>().ResentWorkFolderPaths;

			AddResent(folderPath, resentPaths);

			Settings.Edit<ProgramSettings>(s => { s.ResentWorkFolderPaths = resentPaths; });
		}


		private static void AddResent(string text, List<string> textList)
		{
			int index = textList.FindIndex(path => path.SameIc(text));

			if (index != -1)
			{
				textList.RemoveAt(index);
			}

			textList.Insert(0, text);

			if (textList.Count > 10)
			{
				textList.RemoveRange(10, textList.Count - 10);
			}
		}
	}
}