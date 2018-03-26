namespace GitMind.Utils.Git
{
	internal interface IGitAskPassService
	{
		bool TryHandleRequest();
	}
}