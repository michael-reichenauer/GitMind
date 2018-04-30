using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using GitMind.Common.ThemeHandling;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews.Open
{
	internal class OpenRepoViewModel : ViewModel
	{
		private static readonly Rect DefaultOpenBounds = new Rect(30, 30, 730, 580);

		private readonly IOpenRepoService openRepoService;
		private readonly IRecentReposService recentReposService;
		private readonly IThemeService themeService;


		public OpenRepoViewModel(
			IOpenRepoService openRepoService,
			IRecentReposService recentReposService,
			IThemeService themeService)
		{
			this.openRepoService = openRepoService;
			this.recentReposService = recentReposService;
			this.themeService = themeService;
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

		public Brush HoverBrush => themeService.Theme.HoverBrush;


		public async void OpenRepoAsync() => await openRepoService.OpenRepoAsync();


		private IReadOnlyList<FileItem> GetRecentFiles()
		{
			IReadOnlyList<string> filesPaths = recentReposService.GetWorkFolderPaths();

			var fileItems = new List<FileItem>();
			foreach (string filePath in filesPaths)
			{
				string name = Path.GetFileName(filePath);

				fileItems.Add(new FileItem(name, filePath, openRepoService.OpenOtherRepoAsync));
			}

			return fileItems;
		}


		public async void CloneRepoAsync() => await openRepoService.CloneRepoAsync();

		public async void InitRepoAsync() => await openRepoService.InitRepoAsync();

	}
}