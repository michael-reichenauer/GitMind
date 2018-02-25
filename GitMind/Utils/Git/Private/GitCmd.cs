using System;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitCmd : IGitCmd
	{
		private readonly ICmd2 cmd;


		public GitCmd(ICmd2 cmd)
		{
			this.cmd = cmd;
		}

		private static string CmdPath => @"C:\Work Files\GitMinimal\tools\cmd\git.exe";
		private static string WorkFolder => @"C:\Work Files\AcmAcs";




		public async Task<CmdResult2> RunAsync(string args, CancellationToken ct)
		{
			Timing t = Timing.StartNew();
			CmdOptions options = new CmdOptions { WorkingDirectory = WorkFolder };
			CmdResult2 result = await cmd.RunAsync(CmdPath, args, options, ct);
			t.Log($"{result}");
			return result;
		}


		public async Task<CmdResult2> RunAsync(
			string args, Action<string> outputLines, CancellationToken ct)
		{
			Timing t = Timing.StartNew();
			CmdOptions options = new CmdOptions
			{
				WorkingDirectory = WorkFolder,
				OutputLines = outputLines,
				IsOutputDisabled = true,
			};

			CmdResult2 result = await cmd.RunAsync(CmdPath, args, options, ct);
			t.Log($"{result}");
			return result;
		}
	}
}