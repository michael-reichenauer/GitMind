using System;
using System.Collections.Generic;
using System.Windows.Media;
using GitMind.Git;
using GitMind.GitModel;


namespace GitMind.Common.ThemeHandling
{
	internal class Theme
	{
		private static readonly int MasterBranchBrushIndex = 0;
		private static readonly int MultiBranchBrushIndex = 1;


		public SolidColorBrush BackgroundBrush { get; }
		public SolidColorBrush ForegroundBrush { get; }


		public Brush SubjectBrush { get; } = Converter.BrushFromHex("#D4D4D4");
		public Brush LocalAheadBrush { get; } = Converter.BrushFromHex("#8FE78F"); // Brushes.LightGreen;
		public Brush RemoteAheadBrush { get; } = Converter.BrushFromHex("#BBBBFB");

		public Brush ConflictBrush { get; } = Converter.BrushFromHex("#FCB9B6");
		public Brush MergeBrush { get; } = System.Windows.Media.Brushes.Gold;

		public Brush UnCommittedBrush { get; } = System.Windows.Media.Brushes.BurlyWood;
		public Brush BranchTipBrush { get; } = System.Windows.Media.Brushes.Aqua;

		public Brush TagBrush { get; } = Converter.BrushFromHex("#42C650");
		public Brush TextBrush { get; } = Converter.BrushFromHex("#D4D4D4");
		public Brush TicketBrush { get; } = Converter.BrushFromHex("#F25B54");
		public Brush DimBrush { get; } = System.Windows.Media.Brushes.DimGray;
		public Brush HoverBrush { get; } = Converter.BrushFromHex("#996495ED");

		public Brush ErrorBrush { get; } = Converter.BrushFromHex("#FCB9B6");


		public readonly List<Brush> brushes = new List<Brush>();
		public readonly List<Brush> darkBrushes = new List<Brush>();
		public readonly List<Brush> lighterBrushes = new List<Brush>();
		public readonly List<Brush> lighterLighterBrushes = new List<Brush>();


		public Theme(ThemeOption themeOption)
		{
			BackgroundBrush = Converter.BrushFromHex(themeOption.BackgroundColor);
			ForegroundBrush = Converter.BrushFromHex(themeOption.ForegroundColor);

			LoadThemeBranchColors(themeOption);
		}


		public Brush GetMasterBranchBrush()
		{
			return brushes[MasterBranchBrushIndex];
		}

		public Brush GetMultiBranchBrush()
		{
			return brushes[MultiBranchBrushIndex];
		}

		public Brush GetDarkerBrush(Brush brush)
		{
			int index = brushes.IndexOf(brush);

			return darkBrushes[index];
		}


		public Brush GetBrush(BranchName name)
		{
			int branchBrushId = (Math.Abs(name.GetHashCode()) % (brushes.Count - 2)) + 2;
			return brushes[branchBrushId];
		}


		public Brush GetLighterBrush(Brush brush)
		{
			int index = brushes.IndexOf(brush);

			return lighterBrushes[index];
		}

		public Brush GetLighterLighterBrush(Brush brush)
		{
			int index = brushes.IndexOf(brush);

			return lighterLighterBrushes[index];
		}


		private void LoadThemeBranchColors(ThemeOption themeOption)
		{
			foreach (string hexColor in themeOption.BranchColors.Colors)
			{
				SolidColorBrush brush = Converter.BrushFromHex(hexColor);

				SolidColorBrush darkerBrush = DarkBrush(brush);
				SolidColorBrush lighterBrush = LightBrush(brush);
				SolidColorBrush lighterLighterBrush = LightBrush(lighterBrush);

				brushes.Add(brush);
				darkBrushes.Add(darkerBrush);
				lighterBrushes.Add(lighterBrush);
				lighterLighterBrushes.Add(lighterLighterBrush);
			}
		}


		private SolidColorBrush DarkBrush(SolidColorBrush brush)
		{
			SolidColorBrush darkerBrush = new SolidColorBrush(brush.Color);
			darkerBrush.Color = Converter.InterpolateColors(brush.Color, BackgroundBrush.Color, 0.6f);
			return darkerBrush;
		}


		private SolidColorBrush LightBrush(SolidColorBrush brush)
		{
			SolidColorBrush lighterBrush = new SolidColorBrush(brush.Color);
			lighterBrush.Color = Converter.InterpolateColors(brush.Color, ForegroundBrush.Color, 0.2f);
			return lighterBrush;
		}
	}
}