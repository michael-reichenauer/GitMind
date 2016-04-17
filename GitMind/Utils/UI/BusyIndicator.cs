using System;
using System.Threading.Tasks;
using System.Windows.Threading;


namespace GitMind.Utils.UI
{
	internal class BusyIndicator : Property<string>
	{
		private static readonly string[] indicators = { "o", "o o", "o o o", "o o o o" };
		//private static readonly string[] indicators = { ".", ". .", ". . .", ". . . ." };
		private static readonly TimeSpan InitialIndicatorTime = TimeSpan.FromMilliseconds(100);
		private static readonly TimeSpan IndicatorInterval = TimeSpan.FromMilliseconds(500);

		private readonly DispatcherTimer timer = new DispatcherTimer();
		private int taskCount;
		private int indicatorIndex;
	

		public BusyIndicator(string propertyName, ViewModel viewModel)
			: base(propertyName, viewModel)
		{
			timer.Tick += UpdateIndicator;
		}


		public string Text => Get();


		public void Add(Task task)
		{
			if (task.IsCompleted)
			{
				return;
			}

			taskCount++;

			if (taskCount == 1)
			{
				StartIndicator();
			}

			task.ContinueWith(t => RemoveTask(), TaskScheduler.FromCurrentSynchronizationContext());
		}


		private void RemoveTask()
		{
			taskCount--;
		}


		private void StartIndicator()
		{	
			timer.Interval = InitialIndicatorTime;
			timer.Start();
		}


		private void UpdateIndicator(object sender, EventArgs e)
		{
			if (taskCount > 0)
			{
				string indicatorText = indicators[indicatorIndex];
				Set(indicatorText);
				indicatorIndex = (indicatorIndex + 1) % indicators.Length;

				timer.Interval = IndicatorInterval;
			}
			else
			{
				timer.Stop();
				indicatorIndex = 0;
				Set("");
			}
		}
	}
}