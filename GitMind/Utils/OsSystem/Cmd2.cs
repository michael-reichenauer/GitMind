using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.OsSystem
{
	/// <summary>
	/// Used to run external shell commands/programs. 
	/// </summary>	
	public class Cmd2 : ICmd2
	{
		//	/// <summary>
		//	/// Runs the specified command and returns the output
		//	/// </summary>
		//	public async Task<string> RunAsync(
		//		string command,
		//		string arguments = null,
		//		string workingDirectory = null,
		//		Action<string> outputProgress = null,
		//		Action<string> errorProgress = null,
		//		CancellationToken ct = default(CancellationToken))
		//	{
		//		CmdResult result = await RunCmdAsync(
		//			command,
		//			arguments,
		//			workingDirectory,
		//			outputProgress,
		//			errorProgress,
		//			CancellationToken.None);

		//		return result.Output;
		//	}


		/// <summary>
		/// Runs the specified command and returns detailed process result.
		/// </summary>
		public async Task<CmdResult> RunAsync(
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
			int processExitCode = -1;

			try
			{
				outputProgress = outputProgress ?? (line => { });
				errorProgress = errorProgress ?? (line => { });
				command = Quote(command);

				// Two streams (output and error) and one process needs to complete
				AsyncCountdownEvent completeEvent = new AsyncCountdownEvent(3);

				process = StartProcess(
					command,
					arguments,
					workingDirectory,
					outputLine => ReportLine(outputText, outputLine, outputProgress, completeEvent),
					errorLine => ReportLine(errorText, errorLine, errorProgress, completeEvent),
					exitCode => { processExitCode = exitCode; completeEvent.Signal(); });

				ct.Register(() => Kill(process));

				// Await end of output stream, end of error stream and exit code 
				await completeEvent.WaitAsync(ct);
			}
			catch (Exception e)
			{
				Log.Exception(e, $"Cmd failed: {command} {arguments}");
			}
			finally
			{
				if (process != null)
				{
					process.Dispose();
				}
			}

			return new CmdResult(
				command, arguments, processExitCode, outputText.ToString(), errorText.ToString());
		}


		/// <summary>
		/// Reports the line.
		/// </summary>
		private void ReportLine(
			StringBuilder text,
			string line,
			Action<string> lineProgress,
			AsyncCountdownEvent countdownEvent)
		{
			if (line != null)
			{
				text.AppendLine(line);
				lineProgress(line);
			}
			else
			{
				countdownEvent.Signal();
			}
		}


		/// <summary>
		/// Kill the process.
		/// </summary>
		private static void Kill(Process process)
		{
			try
			{
				process.Kill();
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
		private static Process StartProcess(
			string command,
			string arguments,
			string workingDirectory,
			Action<string> outputLines,
			Action<string> errorLines,
			Action<int> exitCode)
		{
			Process process = new Process();
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.EnableRaisingEvents = true;
			process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
			process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

			process.StartInfo.FileName = command;
			process.StartInfo.Arguments = arguments;
			if (!string.IsNullOrWhiteSpace(workingDirectory))
			{
				process.StartInfo.WorkingDirectory = workingDirectory;
			}

			process.OutputDataReceived += (s, e) => outputLines(e.Data);
			process.ErrorDataReceived += (s, e) => errorLines(e.Data);
			process.Exited += (s, e) => exitCode(process.ExitCode);

			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			return process;
		}
	}
}