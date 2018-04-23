using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.OsSystem
{
	internal interface ICmd2
	{
		CmdResult2 Run(string command, string arguments);

		Task<CmdResult2> RunAsync(string command, string arguments, CancellationToken ct);


		Task<CmdResult2> RunAsync(
			string command,
			string arguments,
			CmdOptions options,
			CancellationToken ct);
	}
}