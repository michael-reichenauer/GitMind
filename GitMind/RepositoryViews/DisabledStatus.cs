using System;


namespace GitMind.RepositoryViews
{
	internal class DisabledStatus : IDisposable
	{
		private readonly RepositoryViewModel repositoryViewModel;



		public DisabledStatus(
			RepositoryViewModel repositoryViewModel)
		{
			this.repositoryViewModel = repositoryViewModel;
			repositoryViewModel.SetIsInternalDialog(true);
		}


		public void Dispose()
		{
			repositoryViewModel.SetIsInternalDialog(false);
			repositoryViewModel.SetMainWindowFocus();
		}
	}
}