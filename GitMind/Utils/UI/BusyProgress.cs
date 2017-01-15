using System;


namespace GitMind.Utils.UI
{
	internal class BusyProgress : IDisposable
	{
		private readonly BusyIndicator busyIndicator;
		private readonly string statusText;
		private Timing timing;

		public BusyProgress(BusyIndicator busyIndicator, string statusText)
		{
			timing = new Timing();
			timing.Log("Start busy indicator ...");
			this.busyIndicator = busyIndicator;
			this.statusText = statusText;
		}


		public void Dispose()
		{
			timing.Log("Done busy indicator");
			busyIndicator.Done(statusText);
		}
	}
}