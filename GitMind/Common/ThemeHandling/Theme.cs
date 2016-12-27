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

		public Brush SubjectBrush { get; }
		public Brush LocalAheadBrush { get; } 
		public Brush RemoteAheadBrush { get; } 
		public Brush ConflictBrush { get; }
		public Brush MergeBrush { get; } 
		public Brush UnCommittedBrush { get; }
		public Brush BranchTipBrush { get; }
		public Brush DimBrush { get; }

		public Brush TagBrush { get; }
		public Brush TextBrush { get; }
		public Brush TicketBrush { get; }

		public Brush HoverBrush { get; }
		public Brush ErrorBrush { get; }


		public readonly List<Brush> brushes = new List<Brush>();
		public readonly List<Brush> darkBrushes = new List<Brush>();
		public readonly List<Brush> lighterBrushes = new List<Brush>();
		public readonly List<Brush> lighterLighterBrushes = new List<Brush>();


		public Theme(ThemeOption option)
		{
			BackgroundBrush = FromHex(option.BackgroundColor);
			ForegroundBrush = FromHex(option.ForegroundColor);
			TextBrush = FromHex(option.TextColor);
			HoverBrush = FromHex(option.HoverColor);
			ErrorBrush = FromHex(option.ErrorColor);

			SubjectBrush = FromHex(option.SubjectColors.SubjectColor);
			LocalAheadBrush = FromHex(option.SubjectColors.LocalAheadColor);
			RemoteAheadBrush = FromHex(option.SubjectColors.RemoteAheadColor);
			ConflictBrush = FromHex(option.SubjectColors.ConflictColor);
			MergeBrush = FromHex(option.SubjectColors.MergeColor);
			UnCommittedBrush = FromHex(option.SubjectColors.UnCommittedColor);
			BranchTipBrush = FromHex(option.SubjectColors.BranchTipColor);
			DimBrush = FromHex(option.SubjectColors.DimColor);

			TagBrush = FromHex(option.SubjectColors.TagColor);
			TicketBrush = FromHex(option.SubjectColors.TicketColor);		

			LoadThemeBranchColors(option);
		}


		private static SolidColorBrush FromHex(string hex)
		{
			return Converter.BrushFromHex(hex);
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