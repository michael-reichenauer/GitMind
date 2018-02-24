using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	public class GitCmd : IGitCmd
	{
		private readonly ICmd2 cmd;


		public GitCmd(ICmd2 cmd)
		{
			this.cmd = cmd;
		}


		public async Task<CmdResult2> DoAsync(string args, CancellationToken ct)
		{
			string cmdPath = @"C:\Work Files\GitMinimal\tools\cmd\git.exe";
			string workFolder = @"C:\Work Files\AcmAcs";

			//void OnOutput(string s) => Log.Debug($"{s}");
			//void OnError(string s) => Log.Warn($"{s}");

			Timing t = Timing.StartNew();

			CmdResult2 result = await cmd.RunAsync(cmdPath, args, workFolder, null, null, ct);
			t.Log($"{result}");
			return result;
		}
	}
}