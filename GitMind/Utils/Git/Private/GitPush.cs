using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git.Private
{
	internal class GitPush : IGitPush
	{
		private readonly IGitCmd gitCmd;

		private static readonly string PushArgs = "push --porcelain origin";


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


		public async Task<GitResult> PushBranchAsync(string branchName, CancellationToken ct)
		{
			string[] refspecs = { $"refs/heads/{branchName}:refs/heads/{branchName}" };

			return await PushRefsAsync(refspecs, ct);
		}


		public async Task<GitResult> PushTagAsync(string tagName, CancellationToken ct)
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

				string pushTagArgs = $"{PushArgs} {tagName}";
				return await gitCmd.RunAsync(pushTagArgs, options, ct);
			}
		}


		public async Task<GitResult> PushRefsAsync(IEnumerable<string> refspecs, CancellationToken ct)
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

				string refsText = string.Join(" ", refspecs);
				string pushRefsArgs = $"{PushArgs} {refsText}";

				return await gitCmd.RunAsync(pushRefsArgs, options, ct);
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