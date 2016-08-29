using System.Net;


namespace GitMind.Git
{
	internal interface ICredentialHandler
	{
		NetworkCredential GetCredential(string url, string usernameFromUrl);
	}
}