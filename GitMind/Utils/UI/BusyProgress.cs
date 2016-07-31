using System;


namespace GitMind.Utils.UI
{
	internal class BusyProgress : IDisposable
	{
		private readonly BusyIndicator busyIndicator;
		private readonly string statusText;


		public BusyProgress(BusyIndicator busyIndicator, string statusText)
		{
			this.busyIndicator = busyIndicator;
			this.statusText = statusText;
		}


		public void Dispose()
		{
			busyIndicator.Done(statusText);
		}
	}
}