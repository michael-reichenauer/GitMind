using System;
using System.Threading.Tasks;


namespace GitMind.Utils.Threading
{
	public class AsyncLock
	{
		private readonly AsyncSemaphore semaphore = new AsyncSemaphore(1);

		public async Task<IDisposable> LockAsync()
		{
			await semaphore.WaitAsync();

			return new Releaser(semaphore);
		}

		private class Releaser : IDisposable
		{
			private readonly AsyncSemaphore semaphore;

			public Releaser(AsyncSemaphore semaphore)
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