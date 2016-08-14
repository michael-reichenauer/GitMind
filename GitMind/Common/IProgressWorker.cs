using System;
using System.Threading.Tasks;


namespace GitMind.Common
{
	internal interface IProgressWorker
	{
		Task DoAsync(Action<string> textSetter);
	}
}