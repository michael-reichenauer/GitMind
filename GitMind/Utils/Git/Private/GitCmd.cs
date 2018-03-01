using System;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Utils.OsSystem;


namespace GitMind.Utils.Git.Private
{
	internal class GitCmd : IGitCmd
	{
		private readonly ICmd2 cmd;
		private readonly WorkingFolderPath workingFolder;


		public GitCmd(ICmd2 cmd, WorkingFolderPath workingFolder)
		{
			this.cmd = cmd;
			this.workingFolder = workingFolder;
		}

		private static string CmdPath => @"C:\Work Files\GitMinimal\tools\cmd\git.exe";
		//private static string WorkFolder => @"C:\Work Files\AcmAcs";




		public async Task<CmdResult2> RunAsync(string args, CancellationToken ct)
		{
			Timing t = Timing.StartNew();
			CmdOptions options = new CmdOptions { WorkingDirectory = workingFolder };
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
				WorkingDirectory = workingFolder,
				OutputLines = outputLines,
				IsOutputDisabled = true,
			};

			CmdResult2 result = await cmd.RunAsync(CmdPath, args, options, ct);
			t.Log($"{result}");
			return result;
		}
	}
}