﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.OsSystem.Private
{
	internal class Cmd2 : ICmd2
	{
		private static readonly char[] QuoteChar = "\"".ToCharArray();

		public CmdResult2 Run(string command, string arguments)
		{
			return Task.Run(() => RunAsync(command, arguments, CancellationToken.None)).Result;
		}


		public Task<CmdResult2> RunAsync(string command, string arguments, CancellationToken ct) =>
			RunAsync(command, arguments, new CmdOptions(), ct);


		public async Task<CmdResult2> RunAsync(
			string command, string arguments, CmdOptions options, CancellationToken ct)
		{
			// Make sure we can handle command paths with "space"
			command = Quote(command);

			try
			{
				return await RunProcessAsync(command, arguments, options, ct);
			}
			catch (Exception e)
			{
				Log.Exception(e, $"Cmd failed: {command} {arguments}");
				return new CmdResult2(
					command,
					arguments,
					options.WorkingDirectory,
					-1,
					"",
					$"{e.GetType()}, {e.Message}",
					TimeSpan.Zero,
					ct);
			}
		}


		private static async Task<CmdResult2> RunProcessAsync(
			string command,
			string arguments,
			CmdOptions options,
			CancellationToken ct)
		{
			// Log.Debug($"Runing: {command} {arguments}");
			Stopwatch stopwatch = Stopwatch.StartNew();

			// The task async exit code
			TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

			Process process = new Process();
			process.StartInfo.FileName = command;
			process.StartInfo.Arguments = arguments;

			SetProcessOptions(process, options);

			process.Start();
			process.Exited += (s, e) => SetExitCode(tcs, process);

			ct.Register(() => Cancel(process, tcs));

			Task inputTask = ProcessInputDataAsync(process, options, ct);
			Task<OutData> outputAndErrorTask = ProcessOutDataAsync(process, options, ct);

			await inputTask.ConfigureAwait(false);
			int exitCode = await tcs.Task.ConfigureAwait(false);
			OutData outData = await outputAndErrorTask.ConfigureAwait(false);

			process?.Dispose();
			stopwatch.Start();
			return new CmdResult2(
				command,
				arguments,
				options.WorkingDirectory,
				exitCode,
				outData.Outout,
				outData.Error,
				stopwatch.Elapsed,
				ct);
		}


		private static void SetExitCode(TaskCompletionSource<int> tcs, Process process)
		{
			try
			{
				tcs.TrySetResult(process.ExitCode);
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to set exit code, {e}");
				tcs.TrySetResult(0);
			}
		}


		private static void SetProcessOptions(Process process, CmdOptions options)
		{
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.StartInfo.RedirectStandardInput = options.InputText != null;
			process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
			process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
			process.EnableRaisingEvents = true;
			options.EnvironmentVariables?.Invoke(process.StartInfo.Environment);


			if (!string.IsNullOrWhiteSpace(options.WorkingDirectory))
			{
				process.StartInfo.WorkingDirectory = options.WorkingDirectory;
			}
		}


		private static async Task<OutData> ProcessOutDataAsync(
			Process process, CmdOptions options, CancellationToken ct)
		{
			StringBuilder outputText = options.IsOutputDisabled ? null : new StringBuilder();
			StringBuilder errorText = options.IsErrortDisabled ? null : new StringBuilder();

			Task outputStreamTask = ReadStreamAsync(
				process.StandardOutput, outputText, options.OutputProgress, options.OutputLines, ct);

			Task errorStreamTask = ReadStreamAsync(
				process.StandardError, errorText, options.ErrorProgress, options.ErrorLines, ct);

			Task streamsTask = Task.WhenAll(outputStreamTask, errorStreamTask);

			// Ensure cancel will abandons stream reading
			TaskCompletionSource<bool> canceledTcs = new TaskCompletionSource<bool>();
			ct.Register(() => canceledTcs.TrySetResult(true));
			await Task.WhenAny(streamsTask, canceledTcs.Task);

			string error = (errorText?.ToString() ?? "") +
				(ct.IsCancellationRequested ? "... Canceled" : "");

			return new OutData
			{ Outout = outputText?.ToString() ?? "", Error = error };
		}


		private static Task ReadStreamAsync(
			StreamReader stream,
			StringBuilder text,
			Action<string> texts,
			Action<string> lines,
			CancellationToken ct)
		{
			if (lines != null)
			{
				return ReadLinesAsync(stream, s => Report(text, s, null, lines, ct), ct);
			}
			else
			{
				return ReadStreamAsync(stream, s => Report(text, s, texts, null, ct), ct);
			}
		}


		private static void Report(
			StringBuilder text,
			string textFragment,
			Action<string> texts,
			Action<string> lines,
			CancellationToken ct)
		{
			if (ct.IsCancellationRequested)
			{
				return;
			}

			if (lines != null)
			{
				text?.AppendLine(textFragment);
				lines?.Invoke(textFragment);
			}
			else
			{
				text?.Append(textFragment);
				texts?.Invoke(textFragment);
			}
		}


		private static void Cancel(Process process, TaskCompletionSource<int> tcs)
		{
			try
			{
				int processId = process.Id;
				process?.Close();
				tcs.TrySetResult(-1);

				// This is probaly not needed, but to just to be sure
				process = Process.GetProcessById(processId);
				process?.Kill();
			}
			catch (Exception)
			{
				// Log.Error($"Exception {e}");
				// Ignore errors. Either there was a running process, which we could kill,
				// or there is no running process to kill. It may have already stopped or not started yet
			}
		}


		private static Task ProcessInputDataAsync(
			Process process, CmdOptions options, CancellationToken ct)
		{
			if (options.InputText == null)
			{
				return Task.CompletedTask;
			}

			return Task.Run(() =>
			{
				using (process.StandardInput)
				{
					while (!ct.IsCancellationRequested)
					{
						string text = options.InputText(ct);
						if (string.IsNullOrEmpty(text))
						{
							break;
						}

						process.StandardInput.WriteLine(text);
					}
				}
			},
			ct);
		}


		private static async Task ReadStreamAsync(
			StreamReader stream, Action<string> onText, CancellationToken ct)
		{
			// Log.Warn("Read stream");
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

		private static async Task ReadLinesAsync(
			StreamReader stream, Action<string> onLine, CancellationToken ct)
		{
			// Log.Warn("Read lines");
			using (StreamReader reader = stream)
			{
				while (!ct.IsCancellationRequested)
				{
					string line = await reader.ReadLineAsync();
					if (line == null)
					{
						break;
					}

					onLine(line);
				}
			}
		}



		private static string Quote(string text)
		{
			text = text.Trim();
			text = text.Trim(QuoteChar);
			return $"\"{text}\"";
		}


		private class OutData
		{
			public string Outout { get; set; }
			public string Error { get; set; }
		}
	}
}
