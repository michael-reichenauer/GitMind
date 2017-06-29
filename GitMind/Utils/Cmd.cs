using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;


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
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Exception(e, $"Exception for {path} {args}");
				return new CmdResult(-1, EmptyLines, new[] { e.Message });
			}
		}


		public CmdResult Run(string path, string args)
		{
			try
			{
				List<string> lines = new List<string>();
				Log.Debug($"Cmd: {path} {args}");

				var process = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = path,
						Arguments = args,
						UseShellExecute = false,
						RedirectStandardOutput = true,
						CreateNoWindow = true,
						StandardOutputEncoding = Encoding.UTF8
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
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Exception(e, $"Exception for {path} {args}");
				return new CmdResult(-1, EmptyLines, new[] { e.Message });
			}
		}
	}
}