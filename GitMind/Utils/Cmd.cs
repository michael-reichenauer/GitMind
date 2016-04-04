using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace GitMind.Utils
{
	public class Cmd : ICmd
	{
		public int Run(string path, string args, out string output)
		{
			IReadOnlyList<string> lines;
			if (0 == Run(path, args, out lines))
			{
				output = string.Join("\n", lines);
				return 0;
			}

			output = null;
			return -1;
		}

		public int Run(string path, string args, out IReadOnlyList<string> output)
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
				output = lines;

				// Log.Debug($"Cmd exit code: {process.ExitCode} for {path} {args}");
				return process.ExitCode;
			}
			catch (Exception e)
			{
				Log.Error($"Exception for {path} {args}, {e}");
				output = null;
				return -1;
			}
		}
	}
}