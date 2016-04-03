namespace GitMind.Installation
{
	public interface IInstaller
	{
		bool IsNormalInstallation();
		bool IsSilentInstallation();

		void InstallNormal();
		void InstallSilent();

		bool IsNormalUninstallation();
		void UninstallNormal();

		bool IsSilentUninstallation();
		void UninstallSilent();
	}
}