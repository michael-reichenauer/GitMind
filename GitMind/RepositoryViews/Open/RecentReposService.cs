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
		
		public void AddRepoPaths(string modelFilePath)
		{
			AddToResentPathInProgramSettings(modelFilePath);

			jumpListService.AddPath(modelFilePath);
			Changed?.Invoke(this, EventArgs.Empty);
		}


		public IReadOnlyList<string> GetRepoPaths()
		{
			ProgramSettings settings = Settings.Get<ProgramSettings>();

			return settings.ResentWorkFolderPaths.ToList();
		}


		private void AddToResentPathInProgramSettings(string modelFilePath)
		{
			ProgramSettings settings = Settings.Get<ProgramSettings>();

			List<string> resentPaths = settings.ResentWorkFolderPaths;
			int index = resentPaths.FindIndex(path => path.SameIc(modelFilePath));

			if (index != -1)
			{
				resentPaths.RemoveAt(index);
			}

			resentPaths.Insert(0, modelFilePath);

			if (resentPaths.Count > 10)
			{
				resentPaths.RemoveRange(10, resentPaths.Count - 10);
			}

			Settings.Edit<ProgramSettings>(s => { s.ResentWorkFolderPaths = resentPaths; });
		}
	}
}