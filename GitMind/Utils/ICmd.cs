using System.Collections.Generic;


namespace GitMind.Utils
{
	public interface ICmd
	{
		CmdResult Run(string path, string args);
	}
}