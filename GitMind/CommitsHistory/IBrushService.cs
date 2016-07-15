using System.Windows.Media;
using GitMind.GitModel;


namespace GitMind.CommitsHistory
{
	internal interface IBrushService
	{
		SolidColorBrush SubjectBrush { get; }
		SolidColorBrush LocalAheadBrush { get; }
		SolidColorBrush RemoteAheadBrush { get; }
		SolidColorBrush ConflictBrush { get; }
		SolidColorBrush UnCommittedBrush { get; }
		SolidColorBrush BranchTipBrush { get; }

		Brush GetDarkerBrush(Brush brush);
		Brush GetBranchBrush(Branch branch);
		Brush GetBranchBrush(string branchName);
	}
}