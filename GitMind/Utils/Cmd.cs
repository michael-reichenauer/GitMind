using System;
using System.Collections.Generic;
using System.Diagnostics;
using GitMind.Settings;


namespace GitMind.Utils
{
	public class Cmd : ICmd
	{
		private static readonly IReadOnlyList<string> EmptyLines = new string[0];

		public CmdResult Start(string path, string args)
		{
			string targetPath = ProgramPaths.GetInstallFilePath();

			ProcessStartInfo info = new ProcessStartInfo(targetPath);
			info.Arguments = "";
			info.UseShellExecute = true;
			try
			{
				Log.Error($"Starting installed path, {targetPath}");

				Process process = Process.Start(info);
				return new CmdResult(process.ExitCode, EmptyLines, EmptyLines);
			}
			catch (Exception e)
			{
				Log.Error($"Exception for {path} {args}, {e}");
				return new CmdResult(-1, EmptyLines, new[] { e.Message });
			}
		}


		public CmdResult Run(string path, string args)
		{
			try
			{
				List<string> lines = new List<string>();
				// Log.Debug($"Cmd: {path} {args}");

				var process = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = path,
						Arguments = args,
						UseShellExecute = false,
						RedirectStandardOutput = true,
						CreateNoWindow = true
					}
				};

				process.Start();

				while (!process.StandardOutput.EndOfStream)
				{
					string line = process.StandardOutput.ReadLine();
					lines.Add(line);
				}

				process.WaitForExit();

				return new CmdResult(process.ExitCode, lines, EmptyLines);
			}
			catch (Exception e)
			{
				Log.Error($"Exception for {path} {args}, {e}");
				return new CmdResult(-1, EmptyLines, new [] {e.Message});
			}
		}	
	}
}