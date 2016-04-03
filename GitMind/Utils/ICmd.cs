using System.Collections.Generic;


namespace GitMind.Utils
{
	public interface ICmd
	{
		int Run(string path, string args, out string output);

		int Run(string path, string args, out IReadOnlyList<string> output);
	}
}