using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace GitMind.Utils
{
	public class Cmd : ICmd
	{
		private static readonly IReadOnlyList<string> EmptyLines = new string[0];

		public CmdResult Start(string path, string args)
		{
			ProcessStartInfo info = new ProcessStartInfo(path);
			info.Arguments = args;
			info.UseShellExecute = true;
			try
			{
				Process.Start(info);
				return new CmdResult(0, EmptyLines, EmptyLines);
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
				return new CmdResult(-1, EmptyLines, new[] { e.Message });
			}
		}
	}
}