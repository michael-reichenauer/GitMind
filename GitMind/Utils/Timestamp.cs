using System;
using System.Diagnostics;


namespace GitMind.Utils
{
	public class Timestamp
	{
		private readonly Stopwatch stopwatch;
		private TimeSpan lastTimeSpan = TimeSpan.Zero;

		public Timestamp()
		{
			stopwatch = new Stopwatch();
			stopwatch.Start();
		}


		public TimeSpan Stop()
		{
			stopwatch.Stop();
			return stopwatch.Elapsed;
		}


		public TimeSpan Elapsed 
		{
			get
			{
				lastTimeSpan = stopwatch.Elapsed;
				return lastTimeSpan;
			}
		}

		public double ElapsedMs => Elapsed.TotalMilliseconds;

		public TimeSpan Diff
		{
			get
			{
				TimeSpan previous = lastTimeSpan;
				return Elapsed - previous;
			}
		}

		public double DiffMs => Diff.TotalMilliseconds;


		public override string ToString() => $"{(long)ElapsedMs} ms";
	}
}