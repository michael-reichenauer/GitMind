using System;
using System.Collections.Generic;


namespace GitMind.Utils.Git.Private
{
	public class GitOptions
	{
		public string WorkingDirectory { get; set; }

		public Action<string> OutputLines { get; set; }
		public Action<string> ErrorProgress { get; set; }
		public Action<string> ErrorLines { get; set; }
		public bool IsOutputDisabled { get; set; }
		public bool IsErrortDisabled { get; set; }

		public Action<IDictionary<string, string>> EnvironmentVariables { get; set; }
		public bool IsEnableCredentials { get; set; }
	}
}