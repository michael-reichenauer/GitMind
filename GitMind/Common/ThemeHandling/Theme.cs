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
		public SolidColorBrush TitlebarBackgroundBrush { get; }
		public Brush TextBrush { get; }

		public Brush SubjectBrush { get; }
		public Brush LocalAheadBrush { get; } 
		public Brush RemoteAheadBrush { get; } 
		public Brush ConflictBrush { get; }
		public Brush MergeBrush { get; } 
		public Brush UnCommittedBrush { get; }
		public Brush BranchTipsBrush { get; }
		public Brush DimBrush { get; }
		public Brush TagBrush { get; }
		public Brush TagBackgroundBrush { get; }
		public Brush TicketBrush { get; }
		public Brush TicketBackgroundBrush { get;  }

		public Brush HoverBrush { get; }
		public Brush ErrorBrush { get; }
		public Brush BorderBrush { get; }
		public Brush TextLowBrush { get; }
		public Brush UndoBrush { get; }
		public Brush CurrentCommitIndicatorBrush { get; }
		public Brush BusyBrush { get; }
		public Brush ScrollbarBrush { get; }
		public Brush ItemBackgroundBrush { get; }
		public Brush SelectedItemBorderBrush { get; }
		public Brush SelectedItemBackgroundBrush { get; }
		public Brush HoverItemBrush { get; }
		public int NeonEffect { get; }
	


		public readonly List<SolidColorBrush> brushes = new List<SolidColorBrush>();
		public readonly List<Brush> darkBrushes = new List<Brush>();
		public readonly List<Brush> lighterBrushes = new List<Brush>();
		public readonly List<Brush> lighterLighterBrushes = new List<Brush>();


		public Theme(ThemeOption option)
		{
			BackgroundBrush = FromHex(option.BackgroundColor);
			ForegroundBrush = FromHex(option.ForegroundColor);
			TitlebarBackgroundBrush = FromHex(option.TitlebarBackgroundColor);

			BorderBrush = FromHex(option.BorderColor);
			TextBrush = FromHex(option.TextColor);
			TextLowBrush = FromHex(option.TextLowColor);
			HoverBrush = FromHex(option.HoverColor);
			ErrorBrush = FromHex(option.ErrorColor);
			UndoBrush = FromHex(option.UndoColor);
			SubjectBrush = FromHex(option.SubjectColors.SubjectColor);
			LocalAheadBrush = FromHex(option.SubjectColors.LocalAheadColor);
			RemoteAheadBrush = FromHex(option.SubjectColors.RemoteAheadColor);
			ConflictBrush = FromHex(option.SubjectColors.ConflictColor);
			MergeBrush = FromHex(option.SubjectColors.MergeColor);
			UnCommittedBrush = FromHex(option.SubjectColors.UnCommittedColor);
			BranchTipsBrush = FromHex(option.SubjectColors.BranchTipColor);
			DimBrush = FromHex(option.SubjectColors.DimColor);
			CurrentCommitIndicatorBrush = FromHex(option.SubjectColors.CurrentCommitIndicatorBrush);

			TagBrush = FromHex(option.SubjectColors.TagColor);
			TagBackgroundBrush = FromHex(option.SubjectColors.TagBackgroundColor);
			TicketBrush = FromHex(option.SubjectColors.TicketColor);
			TicketBackgroundBrush = FromHex(option.SubjectColors.TicketBackgroundColor);

			BusyBrush = FromHex(option.BusyColor);
			ScrollbarBrush = FromHex(option.ScrollbarColor);

			ItemBackgroundBrush = FromHex(option.ItemBackgroundColor);
			SelectedItemBorderBrush = FromHex(option.SelectedItemBorderColor);
			SelectedItemBackgroundBrush = FromHex(option.SelectedItemBackgroundColor);
			HoverItemBrush = FromHex(option.HoverItemColor);

			NeonEffect = option.NeonEffect;

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
			int index = GetBrushIndex(brush);

			return darkBrushes[index];
		}


		public Brush GetBrush(BranchName name)
		{
			int branchBrushId = (Math.Abs(name.GetHashCode()) % (brushes.Count - 2)) + 2;
			return brushes[branchBrushId];
		}


		public Brush GetLighterBrush(Brush brush)
		{
			int index = GetBrushIndex(brush);

			return lighterBrushes[index];
		}


		public Brush GetLighterLighterBrush(Brush brush)
		{
			int index = GetBrushIndex(brush);

			return lighterLighterBrushes[index];
		}


		public int GetBrushIndex(Brush brush)
		{
			SolidColorBrush colorBrush = brush as SolidColorBrush;
			Color color = colorBrush.Color;

			int index = brushes.FindIndex(b => b.Color == color);
			return index;
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