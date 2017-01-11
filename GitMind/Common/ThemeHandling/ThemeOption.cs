using System.Collections.Generic;


namespace GitMind.Common.ThemeHandling
{
	public class ThemeOption
	{
		public string comment => "Theme settings";

		public string Name { get; set; } = "Custom_1";

		public string BackgroundColor { get; set; } = "#FF0E0E0E";
		public string ForegroundColor { get; set; } = "#FFFFFFFF";
		public string TitlebarBackgroundColor { get; set; } = "#FF0E0E0E";

		public string BorderColor { get; set; } = "#FF5240B7";
		public string TextColor { get; set; } = "#FFD4D4D4";
		public string TextLowColor { get; set; } = "#FFB0C4DE";
		public string HoverColor { get; set; } = "#996495ED";
		public string ErrorColor { get; set; } = "#FFFCB9B6";
		public string UndoColor { get; set; } = "#FFF75B54";

		public string BusyColor { get; set; } = "#FFE540FF";
		public string ScrollbarColor { get; set; } = "#FF9932CC";
		public string ItemBackgroundColor { get; set; } = "#00FFFFFF";
		public string SelectedItemBorderColor { get; set; } = "#FF6495ED";
		public string SelectedItemBackgroundColor { get; set; } = "#000025";
		public string HoverItemColor { get; set; } = "#996495ED";

		public int NeonEffect { get; set; } = 15;

		public SubjectColorsOption SubjectColors { get; set; } = new SubjectColorsOption();


		public class SubjectColorsOption
		{
			public string SubjectColor { get; set; } = "#FFD4D4D4";
			public string LocalAheadColor { get; set; } = "#FF8FE78F";
			public string RemoteAheadColor { get; set; } = "#FFBBBBFB";
			public string ConflictColor { get; set; } = "#FFFCB9B6";
			public string MergeColor { get; set; } = "#FFFFD700";
			public string UnCommittedColor { get; set; } = "#FFDEB887";
			public string BranchTipColor { get; set; } = "#FF00FFFF";
			public string DimColor { get; set; } = "#FF696969";
			public string TagColor { get; set; } = "#FF42C650";
			public string TagBackgroundColor { get; set; } = "#00FFFFFF";
			public string TicketColor { get; set; } = "#FFF25B54";
			public string TicketBackgroundColor { get; set; } = "#00FFFFFF";
			public string CurrentCommitIndicatorBrush { get; set; } = "#FFB0C4DE";
		}


		public BranchColorsOption BranchColors { get; set; } = new BranchColorsOption()
		{
			Colors = new List<string>
			{
				"#FFE540FF", // master branch (violet)
				"#FFFFFFFF", // multi- branch (white)
				"#FFFFB300", // Vivid Yellow
				"#FFA12B8E", // Strong Purple
				"#FFFF6800", // Vivid Orange
				"#FF6892C0", // Very Light Blue
				"#FFDF334E", // Vivid Red
				"#FFCEA262", // Grayish Yellow
				"#FFAD7E62", // Medium Gray

				// The following will not be good for people with defective color vision
				"#FF0FA94E", // Vivid Green
				"#FFF6768E", // Strong Purplish Pink
				"#FF085E95", // Strong Blue
				"#FFFF7A5C", // Strong Yellowish Pink
				"#FF6D568D", // Strong Violet
				"#FFFF8E00", // Vivid Orange Yellow
				"#FFB04B6A", // Strong Purplish Red
				"#FFF4C800", // Vivid Greenish Yellow
				"#FFA5574F", // Strong Reddish Brown
				"#FF93AA00", // Vivid Yellowish Green
				"#FF9C5E2C", // Deep Yellowish Brown
				"#FFF13A13", // Vivid Reddish Orange
				"#FF86A854", // Dark Olive Green
			}
		};
	}


	public class LightThemeOption
	{
		public static ThemeOption Create()
		{
			return new ThemeOption
			{
				Name = "Light",

				BackgroundColor = "#FFFAFAFA",
				ForegroundColor = "#FF080808",
				TitlebarBackgroundColor = "#FFEAEAEA",

				BorderColor = "#FF080808",
				TextColor = "#FF080808",
				TextLowColor = "#FF4E4E4E",
				HoverColor = "#996495ED",
				ErrorColor = "#FFFCB9B6",
				UndoColor = "#FFF75B54",

				BusyColor = "#FF490156",
				ScrollbarColor = "#FF6E6E6E",
				ItemBackgroundColor = "#00FFFFFF",
				SelectedItemBorderColor = "#FF6495ED",
				SelectedItemBackgroundColor = "#FFE8F0FE",
				HoverItemColor = "#996495ED",

				NeonEffect = 0,

				SubjectColors = new ThemeOption.SubjectColorsOption()
				{
					SubjectColor = "#FF080808",
					LocalAheadColor = "#FF009300",
					RemoteAheadColor = "#FF5757EE",
					ConflictColor = "#FFFCB9B6",
					MergeColor = "#FFFFD700",
					UnCommittedColor = "#FFCF7500",
					BranchTipColor = "#FF007878",
					DimColor = "#FFC9C9C9",
					TagColor = "#FFFAFAFA",
					TagBackgroundColor = "#FF005E0C",
					TicketColor = "#FFFAFAFA",
					TicketBackgroundColor = "#FFD30F00",
					CurrentCommitIndicatorBrush = "#FF4E4E4E",
				},


				BranchColors = new BranchColorsOption()
				{
					Colors = new List<string>
					{
						"#FFE540FF", // master branch (violet)
						"#FFFFFFFF", // multi- branch (white)
						"#FFFFB300", // Vivid Yellow
						"#FFA12B8E", // Strong Purple
						"#FFFF6800", // Vivid Orange
						"#FF6892C0", // Very Light Blue
						"#FFDF334E", // Vivid Red
						"#FFCEA262", // Grayish Yellow
						"#FFAD7E62", // Medium Gray

						// The following will not be good for people with defective color vision
						"#FF0FA94E", // Vivid Green
						"#FFF6768E", // Strong Purplish Pink
						"#FF085E95", // Strong Blue
						"#FFFF7A5C", // Strong Yellowish Pink
						"#FF6D568D", // Strong Violet
						"#FFFF8E00", // Vivid Orange Yellow
						"#FFB04B6A", // Strong Purplish Red
						"#FFF4C800", // Vivid Greenish Yellow
						"#FFA5574F", // Strong Reddish Brown
						"#FF93AA00", // Vivid Yellowish Green
						"#FF9C5E2C", // Deep Yellowish Brown
						"#FFF13A13", // Vivid Reddish Orange
						"#FF86A854", // Dark Olive Green
					}
				}
			};
		}
	}
}