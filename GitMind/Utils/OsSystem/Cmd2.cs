using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.OsSystem
{
	/// <summary>
	/// Used to run commands/programs. 
	/// </summary>	
	public class Cmd2 : ICmd2
	{
		/// <summary>
		/// Runs the specified command and returns detailed process result.
		/// </summary>
		public async Task<CmdResult2> RunAsync(
			string command,
			string arguments = null,
			string workingDirectory = null,
			Action<string> outputProgress = null,
			Action<string> errorProgress = null,
			CancellationToken ct = default(CancellationToken))
		{
			Process process = null;
			StringBuilder outputText = new StringBuilder();
			StringBuilder errorText = new StringBuilder();
			int exitCode = -1;

			try
			{
				outputProgress = outputProgress ?? (line => { });
				errorProgress = errorProgress ?? (line => { });
				command = Quote(command);

				process = await StartProcessAsync(
					command,
					arguments,
					workingDirectory,
					outputLine => ReportLine(outputText, outputLine, outputProgress),
					errorLine => ReportLine(errorText, errorLine, errorProgress),
					ct);

				exitCode = process?.ExitCode ?? -1;
			}
			catch (Exception e)
			{
				Log.Exception(e, $"Cmd failed: {command} {arguments}");
				errorText.AppendLine($"{e.GetType()}, {e.Message}");
			}
			finally
			{
				process?.Dispose();
			}

			return new CmdResult2(
				command, arguments, exitCode, outputText.ToString(), errorText.ToString());
		}


		/// <summary>
		/// Reports the line.
		/// </summary>
		private void ReportLine(
			StringBuilder text,
			string textFragment,
			Action<string> lineProgress)
		{
			text.Append(textFragment);
			lineProgress(textFragment);
		}


		/// <summary>
		/// Kill the process.
		/// </summary>
		private static void Kill(Process process)
		{
			try
			{
				process?.Kill();
			}
			catch (Exception)
			{
				// Ignore errors. Either there was a running process, which we could kill,
				// or there is no running process to kill. It may have already stopped or not started yet
			}
		}


		/// <summary>
		/// Quotes the specified text, i.e. surrounds texts with '"' chars
		/// </summary>
		private static string Quote(string text)
		{
			text = text.Trim();
			text = text.Trim("\"".ToCharArray());
			text = "\"" + text + "\"";

			return text;
		}

		/// <summary>
		/// Starts the process.
		/// </summary>
		private static async Task<Process> StartProcessAsync(
			string command,
			string arguments,
			string workingDirectory,
			Action<string> outputLines,
			Action<string> onErrorText,
			CancellationToken ct)
		{
			Process process = new Process();
			process.StartInfo.FileName = command;
			process.StartInfo.Arguments = arguments;

			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
			process.StartInfo.StandardErrorEncoding = Encoding.UTF8;


			if (!string.IsNullOrWhiteSpace(workingDirectory))
			{
				process.StartInfo.WorkingDirectory = workingDirectory;
			}

			process.Start();
			ct.Register(() => Kill(process));

			Task outputStreamTask = ReadStreamAsync(process.StandardOutput, outputLines, ct);
			Task errorStreamTask = ReadStreamAsync(process.StandardError, onErrorText, ct);

			process.WaitForExit();
			await Task.WhenAll(outputStreamTask, errorStreamTask);

			return process;
		}


		private static async Task ReadStreamAsync(
			StreamReader stream, Action<string> onText, CancellationToken ct)
		{
			using (StreamReader reader = stream)
			{
				char[] buffer = new char[1024 * 4];

				while (!ct.IsCancellationRequested)
				{
					int readCount = await reader.ReadAsync(buffer, 0, buffer.Length);
					if (readCount == 0)
					{
						break;
					}

					onText(new string(buffer, 0, readCount));
				}
			}
		}
	}
}