using System.Windows.Media;
using GitMind.DataModel;


namespace GitMind.CommitsHistory
{
	internal interface IBrushService
	{
		SolidColorBrush SubjectBrush { get; }
		SolidColorBrush LocalAheadBrush { get; }
		SolidColorBrush RemoteAheadBrush { get; }

		Brush GetBRanchBrush(IBranch branch);
		Brush GetDarkerBrush(Brush brush);
	}
}