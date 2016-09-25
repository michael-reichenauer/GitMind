using GitMind.Utils;


namespace GitMind.Git
{
	internal interface IGitInfoService
	{
		R<string> GetCurrentRootPath(string folder);

		bool IsSupportedRemoteUrl(string workingFolder);
	}
}