namespace GitMind.Installation
{
	internal interface ICommandLine
	{
		bool IsSilent { get; }
		bool IsInstall { get; }
		bool IsUninstall { get; }
		bool IsRunInstalled { get; }
	}
}