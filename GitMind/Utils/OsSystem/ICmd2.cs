using System;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.OsSystem
{
	public interface ICmd2
	{
		/// <summary>
		/// Runs the specified command and returns detailed process result.
		/// </summary>
		Task<CmdResult2> RunAsync(
			string command,
			string arguments = null,
			string workingDirectory = null,
			Action<string> outputProgress = null,
			Action<string> errorProgress = null,
			CancellationToken ct = default(CancellationToken));
	}
}