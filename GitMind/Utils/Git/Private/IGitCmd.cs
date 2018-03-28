using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.Git.Private
{

	internal class GitOptions
	{
		public string WorkingDirectory { get; set; }
		public Func<CancellationToken, string> InputText { get; set; }
		//public Func<CancellationToken, Task<string>> InputTextAsync { get; set; }
		public Action<string> ErrorProgress { get; set; }
		public Action<string> OutputLines { get; set; }
		public Action<string> ErrorLines { get; set; }
		public bool IsOutputDisabled { get; set; }
		public bool IsErrortDisabled { get; set; }

		public Action<IDictionary<string, string>> EnvironmentVariables { get; set; }
		public bool IsEnableCredentials { get; set; }
	}

	internal interface IGitCmd
	{
		Task<GitResult> RunAsync(string gitArgs, CancellationToken ct);


		Task<GitResult> RunAsync(
			string gitArgs, Action<string> outputLines, CancellationToken ct);


		Task<GitResult> RunAsync(
			string gitArgs, GitOptions options, CancellationToken ct);
	}
}