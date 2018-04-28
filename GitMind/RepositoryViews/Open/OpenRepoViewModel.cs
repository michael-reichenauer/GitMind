using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews.Open
{
	internal class OpenRepoViewModel : ViewModel
	{
		private static readonly Rect DefaultOpenBounds = new Rect(30, 30, 730, 580);

		private readonly IOpenModelService openModelService;
		private readonly IRecentModelsService recentModelsService;


		public OpenRepoViewModel(
			IOpenModelService openModelService,
			IRecentModelsService recentModelsService)
		{
			this.openModelService = openModelService;
			this.recentModelsService = recentModelsService;
			Rect = DefaultOpenBounds;

			RecentFiles = GetRecentFiles();
		}

		public string Type => nameof(OpenRepoViewModel);
		public int ZIndex => 200;
		public Rect Rect { get; set; }
		public double Width => Rect.Width;
		public double Top => Rect.Top;
		public double Left => Rect.Left;
		public double Height => Rect.Height;

		public IReadOnlyList<FileItem> RecentFiles { get; }


		public async void OpenFile() => await openModelService.OpenModelAsync();


		private IReadOnlyList<FileItem> GetRecentFiles()
		{
			IReadOnlyList<string> filesPaths = recentModelsService.GetModelPaths();

			var fileItems = new List<FileItem>();
			foreach (string filePath in filesPaths)
			{
				string name = Path.GetFileName(filePath);

				fileItems.Add(new FileItem(name, filePath, openModelService.OpenOtherModelAsync));
			}

			return fileItems;
		}


		public async void OpenExampleFile()
		{
			await openModelService.OpenOtherModelAsync(Assembly.GetEntryAssembly().Location);
		}
	}
}