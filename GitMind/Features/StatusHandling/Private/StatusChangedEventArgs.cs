using System;
using GitMind.Utils.Git;


namespace GitMind.Features.StatusHandling.Private
{
	internal class StatusChangedEventArgs : EventArgs
	{
		public GitStatus NewStatus { get; }

		public DateTime DateTime { get; }


		public StatusChangedEventArgs(GitStatus newStatus, DateTime dateTime)
		{
			NewStatus = newStatus;
			DateTime = dateTime;
		}
	}
}