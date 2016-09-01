using System;
using System.Net;
using System.Threading;


namespace GitMind.Git
{
	internal interface ICredentialHandler
	{
		NetworkCredential GetCredential(string url, string usernameFromUrl);

		void SetConfirm(bool isConfirmed);

		CancellationToken GetTimeoutToken(TimeSpan timeout);
	}
}