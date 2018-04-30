using System;
using System.Collections.Generic;
using System.Linq;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind.RepositoryViews.Open
{
	[SingleInstance]
	internal class RecentReposService : IRecentReposService
	{
		private readonly IJumpListService jumpListService;
		private readonly IGitInfoService gitInfoService;


		public RecentReposService(
			IJumpListService jumpListService,
			IGitInfoService gitInfoService)
		{
			this.jumpListService = jumpListService;
			this.gitInfoService = gitInfoService;
		}


		public event EventHandler Changed;
		
		public void AddWorkFolderPath(string folderPath)
		{
			AddToResentPathInProgramSettings(folderPath);

			jumpListService.AddPath(folderPath);
			Changed?.Invoke(this, EventArgs.Empty);
		}


		public void RemoveWorkFolderPath(string workingFolder)
		{
			List<string> resentPaths = Settings.Get<ProgramSettings>().ResentWorkFolderPaths.ToList();
			int index = resentPaths.FindIndex(path => path.SameIc(workingFolder));

			if (index != -1)
			{
				resentPaths.RemoveAt(index);
			}

			Settings.Edit<ProgramSettings>(s => { s.ResentWorkFolderPaths = resentPaths; });
		}
		


		public IReadOnlyList<string> GetWorkFolderPaths()
		{
			List<string> resentPaths = Settings.Get<ProgramSettings>().ResentWorkFolderPaths.ToList();

			resentPaths = resentPaths
				.Where(path => gitInfoService.GetWorkingFolderRoot(path).Or("").SameIc(path))
				.ToList();

			Settings.Edit<ProgramSettings>(s => { s.ResentWorkFolderPaths = resentPaths; });
			return resentPaths;
		}


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