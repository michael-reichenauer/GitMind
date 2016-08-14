using System;
using System.Windows.Threading;
using GitMind.Utils.UI;


namespace GitMind.Common.ProgressHandling
{
	internal class ProgressDialogViewModel : ViewModel
	{
		private static readonly string indicators = "o o o o o o o o o o o o o o o o o o o o o o ";
		private static readonly TimeSpan InitialIndicatorTime = TimeSpan.FromMilliseconds(10);
		private static readonly TimeSpan IndicatorInterval = TimeSpan.FromMilliseconds(500);

		private readonly DispatcherTimer timer = new DispatcherTimer();
		private int progressCount;
		private int indicatorIndex;


		public ProgressDialogViewModel()
		{
			timer.Tick += UpdateIndicator;
		}


		public string Text
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string IndicatorText
		{
			get { return Get(); }
			private set { Set(value); }
		}


		public void Start()
		{
			progressCount++;

			if (progressCount == 1)
			{
				indicatorIndex = 0;
				timer.Interval = InitialIndicatorTime;
				timer.Start();
			}
		}


		public void Stop()
		{
			progressCount--;

			if (progressCount == 0)
			{
				timer.Stop();
				indicatorIndex = 0;
				IndicatorText = "";
			}
		}


		private void UpdateIndicator(object sender, EventArgs e)
		{
			if (progressCount > 0)
			{
				string indicatorText = indicators;

				indicatorIndex = (indicatorIndex + 1) % (indicators.Length / 2);
				IndicatorText = indicatorText.Substring(0, indicatorIndex * 2);
				timer.Interval = IndicatorInterval;
			}
			else
			{
				timer.Stop();
				indicatorIndex = 0;
				IndicatorText = "";
			}
		}
	}
}