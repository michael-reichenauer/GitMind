using System;
using System.Collections.Generic;


namespace GitMind.Features.StatusHandling.Private
{
	internal class RepoChangedEventArgs : EventArgs
	{
		public DateTime DateTime { get; }

		public IReadOnlyList<string> BranchIds { get; }

		public RepoChangedEventArgs(DateTime dateTime, IReadOnlyList<string> newBranchIds)
		{
			DateTime = dateTime;
			BranchIds = newBranchIds;
		}
	}
}