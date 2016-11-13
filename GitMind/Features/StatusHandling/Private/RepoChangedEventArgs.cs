using System;


namespace GitMind.Features.StatusHandling.Private
{
	internal class RepoChangedEventArgs : EventArgs
	{
		public DateTime DateTime { get; }


		public RepoChangedEventArgs(DateTime dateTime)
		{
			DateTime = dateTime;
		}
	}
}