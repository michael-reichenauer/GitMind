using System.Collections.Generic;
using System.Threading.Tasks;


namespace GitMind.Utils.Threading
{
	/// <summary>
	/// An asynchronous semaphore. Can be used for throttling or handling concurrent access to a
	/// limited number of resources.
	/// </summary>
	/// <remarks>
	/// This class is inspired by http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266983.aspx
	/// </remarks>
	public class AsyncSemaphore
	{
		private static readonly Task Completed = Task.FromResult(true);

		private readonly Queue<TaskCompletionSource<bool>> waiters =
			new Queue<TaskCompletionSource<bool>>();

		private int currentCount;

	
		public AsyncSemaphore(int initialCount)
		{
			currentCount = initialCount;
		}


		public Task WaitAsync()
		{
			lock (waiters)
			{
				if (currentCount > 0)
				{
					// There's still room left in the semaphore, complete immediately (and synchronously)
					--currentCount;
					return Completed;
				}
				else
				{
					// No room left, create a task that will complete in the future.
					var waiter = new TaskCompletionSource<bool>();
					waiters.Enqueue(waiter);
					return waiter.Task;
				}
			}
		}

	
		public void Release()
		{
			TaskCompletionSource<bool> waiterToRelease = null;

			lock (waiters)
			{
				if (waiters.Count > 0)
				{
					waiterToRelease = waiters.Dequeue();
				}
				else
				{
					// If there aren't any waiters, we simply increment the count.
					++currentCount;
				}
			}

			// Complete the task outside the lock.
			// This avoids running any synchronous continuations while holding the lock.
			if (waiterToRelease != null)
			{
				waiterToRelease.SetResult(true);
			}
		}
	}
}