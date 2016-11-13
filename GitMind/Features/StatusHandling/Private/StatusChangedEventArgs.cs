using System;


namespace GitMind.Features.StatusHandling.Private
{
	internal class StatusChangedEventArgs : EventArgs
	{
		public Status NewStatus { get; }

		public Status OldStatus { get; }

		public DateTime DateTime { get; }


		public StatusChangedEventArgs(Status newStatus, Status oldStatus, DateTime dateTime)
		{
			NewStatus = newStatus;
			OldStatus = oldStatus;
			DateTime = dateTime;
		}
	}
}