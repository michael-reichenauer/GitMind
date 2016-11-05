using System.Windows;


namespace GitMind.MainWindowViews
{
	public interface IMainWindowService
	{
		bool IsNewVersionAvailable { set; }

		Window Owner { get; }

		void SetSearchFocus();

		void SetRepositoryViewFocus();
	}
}