using GitMind.Utils.UI;


namespace GitMind.Utils.Git.Private
{
	internal class GitCredential : IGitCredential
	{
		public GitCredential(CredentialsDialog credentialsDialog)
		{
			Dialog = credentialsDialog;
		}


		public CredentialsDialog Dialog { get; }
		public string Username => Dialog.Name;
		public string Password => Dialog.Password;
	}
}