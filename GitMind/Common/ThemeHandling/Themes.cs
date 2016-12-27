using System.Collections.Generic;


namespace GitMind.Common.ThemeHandling
{
	public class Themes
	{
		public string comment0 => "Theme options. You can edit and add custom themes in the list.";
		public string comment1 => "Specify CurrentTheme name.";
		public string comment2 => "Default theme is read-only.";

		public string CurrentTheme = "Default";

		public DefaultTheme DefaultTheme => new DefaultTheme();

		public Theme[] CustomThemes { get; set; } = { new CustomTheme() };
	}

	public class Theme
	{
		public virtual string comment => "Theme settings";

		public virtual string Name { get; set; } = "<name>";

		public virtual BranchColors BranchColors { get; set; } = new BranchColors();
	}

	public class BranchColors
	{
		public virtual string comment => "Branch colors";

		public virtual List<string> Colors { get; set; } = new List<string>();
	}

	public class CustomTheme : Theme
	{
		public override string comment => "Custom examble theme, edit or copy to add more";

		public override string Name { get; set; } = "Custom_1";

		public override BranchColors BranchColors { get; set; } = new DefaultBranchColors();
	}


	public class DefaultTheme : Theme
	{
		public override string comment => "Default theme (read-only)";

		public override string Name { get; set; } = "Default";

		public override BranchColors BranchColors => new DefaultBranchColors();
	}


	public class DefaultBranchColors : BranchColors
	{
		public override string comment => 
			"Branch colors. First color is master branch, second multi branch and rest normal branch colors.";

		public override List<string> Colors => new List<string>
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
		};
	}
}