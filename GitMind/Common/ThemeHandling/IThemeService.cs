using System.Windows;
using System.Windows.Media;
using GitMind.GitModel;


namespace GitMind.Common.ThemeHandling
{
	internal interface IThemeService
	{
		Theme Theme { get; }

		Brush GetBranchBrush(Branch branch);
	
		Brush ChangeBranchBrush(Branch branch);

		void SetThemeWpfColors(ResourceDictionary colors);
	}
}