using System;
using System.Threading.Tasks;


namespace GitMind.Common.ProgressHandling
{
	internal interface IProgressState
	{
		Task DoAsync(Action<string> textSetter);
	}
}