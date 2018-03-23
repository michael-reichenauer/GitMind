using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitPush : IGitPush
	{
		private readonly IGitCmd gitCmd;

		private static readonly string PushArgs = @"push --porcelain";


		public GitPush(IGitCmd gitCmd)
		{
			this.gitCmd = gitCmd;
		}


		public async Task<GitResult> PushAsync(CancellationToken ct)
		{
			using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				ct = cts.Token;

				// In case login failes, we need to detect that 
				GitOptions options = new GitOptions
				{
					ErrorProgress = text => ErrorProgress(text, cts),
					//InputText = text => InputText(text, ct)
				};

				return await gitCmd.RunAsync(PushArgs, options, ct);
			}
		}


		private string InputText(CancellationToken text, CancellationToken ct)
		{
			//await Task.Yield();
			return "x";
		}


		private static void ErrorProgress(string text, CancellationTokenSource cts)
		{
			Log.Debug($"Push error: {text}");
			if (text.Contains("no-gitmind-pswd-prompt"))
			{
				Log.Warn($"Login failed, {text}");
				cts.Cancel();
			}
		}
	}
}