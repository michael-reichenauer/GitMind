using System.Diagnostics;
using System.Threading.Tasks;


namespace GitMind.Utils
{
	public static class RunInbackgroundTaskExtensions
	{
		public static void RunInBackground(this Task task)
		{
			task.ContinueWith(
				LogFailedTask,
				TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted);
		}


		private static void LogFailedTask(Task task)
		{
			Asserter.FailFast($"RunInBackground task failed: {task.Exception?.InnerExceptions}");			
		}
	}
}