using System;


namespace GitMind.Common.ProgressHandling
{
	public class Progress : IDisposable
	{
		public virtual void SetText(string text)
		{		
		}

		public virtual void Dispose()
		{
		}
	}
}