namespace GitMind.Utils.Git
{
	public interface IGitInfo
	{
		R<string> GetWorkingFolderRoot(string path);
	}
}