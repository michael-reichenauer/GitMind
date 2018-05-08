namespace GitMind.Utils.Git.Private.CredentialsHandling
{
	internal interface ICredentialService
	{
		bool TryGetCredential(string url, string username, out IGitCredential gitCredential);

		bool TryGetPassword(string username, out IGitCredential gitCredential);

		bool TryGetPassphrase(string username, out IGitCredential gitCredential);

		void SetDialogConfirm(IGitCredential dialog, bool isConfirmed);
	}
}