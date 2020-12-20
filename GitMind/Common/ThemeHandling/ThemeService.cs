using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using GitMind.ApplicationHandling;
using GitMind.ApplicationHandling.SettingsHandling;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind.Common.ThemeHandling
{
	[SingleInstance]
	internal class ThemeService : IThemeService
	{	
		private readonly WorkingFolder workingFolder;
		private Lazy<IDictionary<string, Brush>> customBranchBrushes;


		private Theme currentTheme;

		public ThemeService(
			WorkingFolder workingFolder)
		{
			this.workingFolder = workingFolder;

			LoadTheme();

			customBranchBrushes = new Lazy<IDictionary<string, Brush>>(GetCustomBranchColors);

			workingFolder.OnChange += (s, e) =>
				customBranchBrushes = new Lazy<IDictionary<string, Brush>>(GetCustomBranchColors);
		}


		public Theme Theme => currentTheme;

		public Brush GetBranchBrush(Branch branch)
		{
			if (branch.IsMultiBranch)
			{
				return currentTheme.GetMultiBranchBrush();
			}

			if (branch.Name == BranchName.Master)
			{
				return currentTheme.GetMasterBranchBrush();
			}

			if (customBranchBrushes.Value.TryGetValue(branch.Name, out Brush branchBrush))
			{
				return branchBrush;
			}

			return currentTheme.GetBrush(branch.Name);
		}


		public Brush ChangeBranchBrush(Branch branch)
		{
			Brush currentBrush = GetBranchBrush(branch);
			int index = currentTheme.GetBrushIndex(currentBrush);
	
			// Select next brush
			int newIndex = ((index + 1) % (currentTheme.brushes.Count - 2)) + 2;

			Brush brush = currentTheme.brushes[newIndex];		
			string brushHex = Converter.HexFromBrush(brush);

			WorkFolderSettings settings = Settings.GetWorkFolderSetting(workingFolder);
			settings.BranchColors[branch.Name] = brushHex;
			Settings.SetWorkFolderSetting(workingFolder, settings);

			customBranchBrushes = new Lazy<IDictionary<string, Brush>>(GetCustomBranchColors);

			return brush;
		}


		public bool SetThemeWpfColors()
		{
			if (!LoadTheme())
			{
				return false;
			}

			Collection<ResourceDictionary> dictionaries = 
				Application.Current.Resources.MergedDictionaries;

			ResourceDictionary colors = dictionaries
				.First(r => r.Source.ToString() == "Styles/ColorStyle.xaml");

			colors["BackgroundBrush"] = Theme.BackgroundBrush;
			colors["TitlebarBackgroundBrush"] = Theme.TitlebarBackgroundBrush;
			colors["BorderBrush"] = Theme.BorderBrush;
			colors["TextBrush"] = Theme.TextBrush;
			colors["TextLowBrush"] = Theme.TextLowBrush;
			colors["TicketBrush"] = Theme.TicketBrush;
			colors["UndoBrush"] = Theme.UndoBrush;
			colors["TagBrush"] = Theme.TagBrush;
			colors["BranchTipsBrush"] = Theme.BranchTipsBrush;
			colors["CurrentCommitIndicatorBrush"] = Theme.CurrentCommitIndicatorBrush;
			colors["RemoteAheadBrush"] = Theme.RemoteAheadBrush;
			colors["LocalAheadBrush"] = Theme.LocalAheadBrush;
			colors["BusyBrush"] = Theme.BusyBrush;
			colors["ScrollbarBrush"] = Theme.ScrollbarBrush;
			colors["ConflictBrush"] = Theme.ConflictBrush;
			colors["UncomittedBrush"] = Theme.UnCommittedBrush;

			colors["ItemBrush"] = Theme.ItemBackgroundBrush;
			colors["SelectedItemBorderBrush"] = Theme.SelectedItemBorderBrush;
			colors["SelectedItemBackgroundBrush"] = Theme.SelectedItemBackgroundBrush;
			colors["HoverItemBrush"] = Theme.HoverItemBrush;

			return true;
		}


		private bool LoadTheme()
		{
			try
			{
				ThemeOption themeOption = GetCurrentThemeOption();

				currentTheme = new Theme(themeOption);
				return true;
			}
			catch (Exception e)
			{
				Log.Exception(e, "Error in theme option");
				currentTheme = new Theme(new ThemeOption());
				return false;
			}			
		}

		
		private static ThemeOption GetCurrentThemeOption()
		{
			Options options = Settings.Get<Options>();

			ThemeOption theme = options.Themes.CustomThemes
				.FirstOrDefault(t => t.Name == options.Themes.CurrentTheme)
				?? options.Themes.DefaultTheme;

			return theme;
		}


		private IDictionary<string, Brush> GetCustomBranchColors()
		{
			Dictionary<string, Brush> brushes = new Dictionary<string, Brush>();

			WorkFolderSettings settings = Settings.GetWorkFolderSetting(workingFolder);

			foreach (var pair in settings.BranchColors)
			{
				Brush brush = currentTheme.brushes.FirstOrDefault(b => Converter.HexFromBrush(b) == pair.Value);
				if (brush != null)
				{
					brushes[pair.Key] = brush;
				}
			}

			return brushes;
		}
	}
}