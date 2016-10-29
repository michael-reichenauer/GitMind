namespace GitMind.ApplicationHandling.Installation
{
	public interface IInstaller
	{
		void InstallNormal();

		void InstallSilent();

		void UninstallNormal();

		void UninstallSilent();

		void StartInstalled();
	}
}