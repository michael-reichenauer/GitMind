namespace GitMind.Utils.Git.Private
{
	internal interface IGitCredential
	{
		string Username { get; }
		string Password { get; }
		string Url { get; }
	}
}