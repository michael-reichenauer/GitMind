using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using GitMind.ApplicationHandling;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.Git;
using GitMind.GitModel;
using GitMind.Utils;


namespace GitMind.Common.ThemeHandling
{
	[SingleInstance]
	internal class ThemeService : IThemeService
	{
	
		private readonly WorkingFolder workingFolder;
		private readonly Dictionary<string, Brush> customBranchBrushes = new Dictionary<string, Brush>();

		private Theme theme;


		public ThemeService(WorkingFolder workingFolder)
		{
			this.workingFolder = workingFolder;

			LoadTheme();

			LoadCustomBranchColors();

			workingFolder.OnChange += (s, e) => LoadCustomBranchColors();
		}


		public Theme Theme => theme;

		public Brush GetBranchBrush(Branch branch)
		{
			if (branch.IsMultiBranch)
			{
				return theme.GetMultiBranchBrush();
			}

			if (branch.Name == BranchName.Master)
			{
				return theme.GetMasterBranchBrush();
			}

			if (customBranchBrushes.TryGetValue(branch.Name, out Brush branchBrush))
			{
				return branchBrush;
			}

			return theme.GetBrush(branch.Name);
		}




		public Brush ChangeBranchBrush(Branch branch)
		{
			Brush currentBrush = GetBranchBrush(branch);
			int index = theme.brushes.IndexOf(currentBrush);
	
			// Select next brush
			int newIndex = ((index + 1) % (theme.brushes.Count - 2)) + 2;

			Brush brush = theme.brushes[newIndex];		
			string brushHex = Converter.HexFromBrush(brush);

			WorkFolderSettings settings = Settings.GetWorkFolderSetting(workingFolder);
			settings.BranchColors[branch.Name] = brushHex;
			Settings.SetWorkFolderSetting(workingFolder, settings);

			LoadCustomBranchColors();

			return brush;
		}




		private void LoadTheme()
		{
			ThemeOption themeOption = GetCurrentThemeOption();

			theme = new Theme(themeOption);
		}

		
		private static ThemeOption GetCurrentThemeOption()
		{
			Options options = Settings.Get<Options>();

			ThemeOption theme = options.Themes.CustomThemes
				.FirstOrDefault(t => t.Name == options.Themes.CurrentTheme)
				?? options.Themes.DefaultTheme;

			return theme;
		}


		private void LoadCustomBranchColors()
		{
			customBranchBrushes.Clear();
			WorkFolderSettings settings = Settings.GetWorkFolderSetting(workingFolder);

			foreach (var pair in settings.BranchColors)
			{
				Brush brush = theme.brushes.FirstOrDefault(b => Converter.HexFromBrush(b) == pair.Value);
				if (brush != null)
				{
					customBranchBrushes[pair.Key] = brush;
				}
			}
		}
	}
}