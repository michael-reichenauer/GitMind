namespace GitMind.Features.Remote
{
	internal interface IRemoteService
	{
		void TryUpdateAllBranches();
		bool CanExecuteTryUpdateAllBranches();
	}
}