using System;


namespace GitMind.Common.ProgressHandling
{
	internal class Progress : IDisposable
	{
		public virtual void SetText(string text)
		{		
		}

		public virtual void Dispose()
		{
		}
	}
}