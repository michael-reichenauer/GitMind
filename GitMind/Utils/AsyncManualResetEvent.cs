using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace GitMind.Utils
{
	/// <summary>
	/// An asynchronous manual reset event that releases all waiters when it is set, 
	/// and remains set until it is reset.
	/// </summary>
	/// <remarks>This class is inspired by 
	/// blogs.msdn.com/b/pfxteam/archive/2012/02/11/10266920.aspx. 
	/// </remarks>
	public class AsyncManualResetEvent
	{
		private readonly object syncRoot = new object();

		/// <summary> The list of waiters when the event is not set. </summary>
		private readonly List<TaskCompletionSource<object>> waiters =
			new List<TaskCompletionSource<object>>();

		private bool isSet = false;

		public Task WaitAsync(CancellationToken ct)
		{
			lock (syncRoot)
			{
				if (isSet)
				{
					return Task.FromResult(true);
				}
				else
				{
					var waiter = new TaskCompletionSource<object>();
					waiters.Add(waiter);

					// NOTE: The CancellationTokenRegistration is not disposed because of the weird 
					// ObjectDisposedException that may be thrown by CancellationTokenRegistration.Dispose in
					// .NET 4.0. Re-consider this after switching to .NET 4.5.
					ct.Register(() => CancellationRequested(waiter));

					return waiter.Task;
				}
			}
		}


		public void Set()
		{
			IList<TaskCompletionSource<object>> waitersToRelease;

			lock (syncRoot)
			{
				isSet = true;
				waitersToRelease = waiters.ToArray();
				waiters.Clear();
			}

			foreach (TaskCompletionSource<object> waiterToRelease in waitersToRelease)
			{
				waiterToRelease.SetResult(null);
			}
		}


		public void Reset()
		{
			lock (syncRoot)
			{
				isSet = false;
			}
		}


		private void CancellationRequested(TaskCompletionSource<object> waiter)
		{
			bool doCancel = false;

			lock (syncRoot)
			{
				if (waiters.Contains(waiter))
				{
					waiters.Remove(waiter);
					doCancel = true;
				}
			}

			if (doCancel)
			{
				waiter.SetCanceled();
			}
		}
	}
}