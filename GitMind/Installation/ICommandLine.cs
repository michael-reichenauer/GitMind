namespace GitMind.Installation
{
	internal interface ICommandLine
	{
		bool IsNormalInstallation();
		bool IsSilentInstallation();
		bool IsNormalUninstallation();
		bool IsSilentUninstallation();
	}
}