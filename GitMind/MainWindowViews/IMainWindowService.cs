namespace GitMind.MainWindowViews
{
	public interface IMainWindowService
	{
		bool IsNewVersionAvailable { set; }

		void SetSearchFocus();

		void SetRepositoryViewFocus();

		void SetMainWindowFocus();
	}
}