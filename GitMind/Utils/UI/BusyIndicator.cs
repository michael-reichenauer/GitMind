using System;
using System.Windows.Threading;


namespace GitMind.Utils.UI
{
	internal class BusyIndicator
	{
		private readonly string propertyName;
		private readonly ViewModel viewModel;
		private static readonly string[] indicators = { "o", "o o", "o o o", "o o o o" };
		private static readonly TimeSpan InitialIndicatorTime = TimeSpan.FromMilliseconds(10);
		private static readonly TimeSpan IndicatorInterval = TimeSpan.FromMilliseconds(500);

		private readonly DispatcherTimer timer = new DispatcherTimer();
		private int progressCount;
		private int indicatorIndex;


		public BusyIndicator(string propertyName, ViewModel viewModel)
		{
			this.propertyName = propertyName;
			this.viewModel = viewModel;
			timer.Tick += UpdateIndicator;
		}


		public string Text { get; private set; }


		public BusyProgress Progress
		{
			get
			{
				StartIndicator();

				return new BusyProgress(this);
			}
		}


		public void Done()
		{
			progressCount--;

			if (progressCount == 0)
			{
				timer.Stop();
				indicatorIndex = 0;
				Set("");
			}
		}


		private void StartIndicator()
		{
			progressCount++;

			if (progressCount == 1)
			{
				indicatorIndex = 0;
				timer.Interval = InitialIndicatorTime;
				timer.Start();
			}
		}


		private void UpdateIndicator(object sender, EventArgs e)
		{
			if (progressCount > 0)
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


		private void Set(string indicatorText)
		{
			Text = indicatorText;
			viewModel.OnPropertyChanged(propertyName);
		}


		//public void Add(Task task)
		//{
		//	if (task.IsCompleted)
		//	{
		//		return;
		//	}

		//	taskCount++;

		//	if (taskCount == 1)
		//	{
		//		StartIndicator();
		//	}

		//	task.ContinueWith(t => Done(), TaskScheduler.FromCurrentSynchronizationContext());
		//}

	}
}