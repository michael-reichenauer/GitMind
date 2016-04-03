using System;
using System.Diagnostics;


namespace GitMind.Utils
{
	public class Timestamp
	{
		private readonly Stopwatch stopwatch;
		public Timestamp()
		{
			stopwatch = new Stopwatch();
			stopwatch.Start();
		}

		public TimeSpan Elapsed
		{
			get
			{
				stopwatch.Stop();
				return stopwatch.Elapsed;
			}
		}

	
		public TimeSpan Current => stopwatch.Elapsed;
		public double CurrentMs => Current.TotalMilliseconds;

		public override string ToString() => $"{(long)Elapsed.TotalMilliseconds} ms";
	}
}