namespace GitMind.Utils.Git
{
	public interface IGitInfo
	{
		string TryGetWorkingFolderRoot(string path);
	}
}