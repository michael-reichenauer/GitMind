using GitMind.Utils.Ipc;


namespace GitMind.Utils.Git.Private.CredentialsHandling
{
	internal class CredentialIpcService : IpcService
	{
		private readonly CredentialSession session;

		public CredentialIpcService(CredentialSession session) => this.session = session;

		public string AskPassRequest(string prompt) =>
			session.AskPassRequest(prompt);
	}
}