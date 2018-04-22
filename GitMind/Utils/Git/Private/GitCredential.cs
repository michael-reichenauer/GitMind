using GitMind.Utils.Git.Private.CredentialsHandling;
using GitMind.Utils.UI;


namespace GitMind.Utils.Git.Private
{
	internal class GitCredential : IGitCredential
	{
		public GitCredential(CredentialsDialog credentialsDialog, string url)
		{
			Dialog = credentialsDialog;
			Url = url;
		}


		public CredentialsDialog Dialog { get; }
		public string Url { get; }
		public string Username => Dialog.Name;
		public string Password => Dialog.Password;
	}
}