using System;
using System.Threading.Tasks;


namespace GitMind.Common.ProgressHandling
{
	internal interface IProgressWorker
	{
		Task DoAsync(Action<string> textSetter);
	}
}