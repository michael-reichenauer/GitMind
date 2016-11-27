using System;


namespace GitMind.Features.StatusHandling.Private
{
	public class FileEventArgs : EventArgs
	{
		public DateTime DateTime { get; }

		public FileEventArgs(DateTime dateTime)
		{
			DateTime = dateTime;
		}
	}
}