using System;
using System.Collections.Generic;
using System.Threading;


namespace GitMind.Utils.OsSystem
{
	internal class CmdOptions
	{
		public string WorkingDirectory { get; set; }
		public Func<CancellationToken, string> InputText { get; set; }
		//public Func<CancellationToken, Task<string>> InputTextAsync { get; set; }
		public Action<string> OutputProgress { get; set; }
		public Action<string> ErrorProgress { get; set; }
		public Action<string> OutputLines { get; set; }
		public Action<string> ErrorLines { get; set; }
		public bool IsOutputDisabled { get; set; }
		public bool IsErrortDisabled { get; set; }

		public Action<IDictionary<string, string>> EnvironmentVariables { get; set; }
	}
}