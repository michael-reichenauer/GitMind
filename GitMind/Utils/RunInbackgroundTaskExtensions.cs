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
			Log.Error($"Task failed {task.Exception?.InnerExceptions}");			
		}
	}
}