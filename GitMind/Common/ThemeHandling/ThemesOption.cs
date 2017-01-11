namespace GitMind.Common.ThemeHandling
{
	public class ThemesOption
	{
		public string comment0 => "Theme options. You can edit and add custom themes in the list.";
		public string comment1 => "Specify CurrentTheme name.";
		public string comment2 => "Default theme is read-only.";

		public string CurrentTheme = "Dark";

		public ThemeOption[] CustomThemes { get; set; } =
		{
			new ThemeOption { Name = "Dark" },
			LightThemeOption.Create()	
		};

		public ThemeOption DefaultTheme => new ThemeOption { Name = "Default Dark (read-only)" };		
	}
}