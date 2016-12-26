using System.Windows.Media;
using GitMind.GitModel;


namespace GitMind.Common.Brushes
{
	internal interface IBrushService
	{
		SolidColorBrush SubjectBrush { get; }
		SolidColorBrush LocalAheadBrush { get; }
		SolidColorBrush RemoteAheadBrush { get; }
		SolidColorBrush UnCommittedBrush { get; }

		Brush GetDarkerBrush(Brush brush);
		Brush GetLighterBrush(Brush brush);

		Brush GetBranchBrush(Branch branch);
		Brush GetLighterLighterBrush(Brush brush);

		Brush ChangeBranchBrush(Branch branch);
	}
}