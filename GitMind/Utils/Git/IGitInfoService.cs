namespace GitMind.Utils.Git
{
	public interface IGitInfoService
	{
		R<string> GetWorkingFolderRoot(string path);
	}
}