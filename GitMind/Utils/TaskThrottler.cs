using System;
using System.Threading.Tasks;


namespace GitMind.Utils
{
	public class TaskThrottler
	{
		private readonly AsyncSemaphore semaphore;

		public TaskThrottler(int maxConcurrencyLevel)
		{
			semaphore = new AsyncSemaphore(maxConcurrencyLevel);
		}

		public async Task<TResult> Run<TResult>(Func<Task<TResult>> taskFunction)
		{
			await semaphore.WaitAsync();

			try
			{
				// We don't care which thread release the semaphore
				return await taskFunction().ConfigureAwait(false);
			}
			finally
			{
				semaphore.Release();
			}
		}

		public async Task Run(Func<Task> taskFunction)
		{
			await semaphore.WaitAsync();

			try
			{
				// We don't care which thread release the semaphore
				await taskFunction().ConfigureAwait(false);
			}
			finally
			{
				semaphore.Release();
			}
		}
	}
}