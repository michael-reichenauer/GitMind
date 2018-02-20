using System;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils.OsSystem
{
	/// <summary>
	/// Used async wait for counter to reach 0
	/// </summary>
	public class AsyncCountdownEvent
	{
		private readonly AsyncManualResetEvent amre = new AsyncManualResetEvent();
		private int count;

		public AsyncCountdownEvent(int initialCount)
		{
			count = initialCount;
		}


		public Task WaitAsync(CancellationToken ct) => amre.WaitAsync(ct);

		public void Signal()
		{
			if (count <= 0)
			{
				throw new InvalidOperationException();
			}

			int newCount = Interlocked.Decrement(ref count);
			if (newCount == 0)
			{
				amre.Set();
			}
			else if (newCount < 0)
			{
				throw new InvalidOperationException();
			}
		}

		public Task SignalAndWait(CancellationToken ct)
		{
			Signal();
			return WaitAsync(ct);
		}
	}
}