using System;


namespace GitMind.Utils.UI
{
	internal class BusyProgress : IDisposable
	{
		private readonly BusyIndicator busyIndicator;


		public BusyProgress(BusyIndicator busyIndicator)
		{
			this.busyIndicator = busyIndicator;
		}


		public void Dispose()
		{
			busyIndicator.Done();
		}
	}
}