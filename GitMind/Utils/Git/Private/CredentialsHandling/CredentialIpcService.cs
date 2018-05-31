using GitMind.Utils.Ipc;


namespace GitMind.Utils.Git.Private.CredentialsHandling
{
	/// <summary>
	/// The IPC handler, which received callas via IPC from s GitMind process called by git.exe
	/// in the GitAskPassService class.
	/// </summary>
	internal class CredentialIpcService : IpcService
	{
		private readonly CredentialSession session;

		public CredentialIpcService(CredentialSession session) => this.session = session;

		public string AskPassRequest(string prompt) =>
			session.AskPassRequest(prompt);
	}
}