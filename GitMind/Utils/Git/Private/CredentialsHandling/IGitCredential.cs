namespace GitMind.Utils.Git.Private.CredentialsHandling
{
	internal interface IGitCredential
	{
		string Username { get; }
		string Password { get; }
		string Url { get; }
	}
}