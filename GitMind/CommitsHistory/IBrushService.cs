using System.Windows.Media;
using GitMind.DataModel;
using GitMind.DataModel.Old;
using GitMind.GitModel;


namespace GitMind.CommitsHistory
{
	internal interface IBrushService
	{
		SolidColorBrush SubjectBrush { get; }
		SolidColorBrush LocalAheadBrush { get; }
		SolidColorBrush RemoteAheadBrush { get; }

		Brush GetBranchBrush(IBranch branch);
		Brush GetDarkerBrush(Brush brush);
		Brush GetBranchBrush(Branch branch);
	}
}