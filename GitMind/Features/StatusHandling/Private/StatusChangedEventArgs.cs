using System;
using GitMind.Utils.Git;


namespace GitMind.Features.StatusHandling.Private
{
	internal class StatusChangedEventArgs : EventArgs
	{
		public GitStatus2 NewStatus { get; }

		public DateTime DateTime { get; }


		public StatusChangedEventArgs(GitStatus2 newStatus, DateTime dateTime)
		{
			NewStatus = newStatus;
			DateTime = dateTime;
		}
	}
}