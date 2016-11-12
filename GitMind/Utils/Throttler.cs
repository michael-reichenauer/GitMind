using System;
using System.Threading.Tasks;


namespace GitMind.Utils
{
	public class Throttler
	{
		private readonly AsyncSemaphore semaphore;

		public Throttler(int maxConcurrencyLevel)
		{
			semaphore = new AsyncSemaphore(maxConcurrencyLevel);
		}

		public async Task<IDisposable> EnterAsync()
		{
			await semaphore.WaitAsync();

			return new ThrottleState(semaphore);		
		}
		
		private class ThrottleState : IDisposable
		{
			private readonly AsyncSemaphore semaphore;

			public ThrottleState(AsyncSemaphore semaphore)
			{
				this.semaphore = semaphore;
			}

			public void Dispose()
			{
				semaphore.Release();
			}
		}
	}
}