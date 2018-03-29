namespace GitMind.Utils.Git.Private
{
	internal interface ICredentialService
	{
		bool TryGetCredential(string url, string username, out IGitCredential gitCredential);

		void SetDialogConfirm(IGitCredential dialog, bool isConfirmed);
	}
}