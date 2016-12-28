namespace GitMind.Common.ThemeHandling
{
	public class ThemeOption
	{
		public string comment => "Theme settings";

		public string Name { get; set; } = "Custom_1";

		public string BackgroundColor { get; set; } = "#FF0E0E0E";
		public string ForegroundColor { get; set; } = "#FFFFFFFF";
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
	

		public BranchColorsOption BranchColors { get; set; } = new BranchColorsOption();
	}
}