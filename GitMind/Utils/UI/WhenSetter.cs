using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;


namespace GitMind.Utils.UI
{
	internal class WhenSetter
	{
		private readonly ViewModel targetViewModel;
		private readonly string[] sourcePropertyNames;

		private bool isNotifyAll = false;
		private IEnumerable<string> targetPropertyNames;

		public WhenSetter(ViewModel targetViewModel, ViewModel sourceViewModel, params string[] sourcePropertyNames)
		{
			this.targetViewModel = targetViewModel;
			this.sourcePropertyNames = sourcePropertyNames;

			sourceViewModel.PropertyChanged += PropertyChanaged;
		}


		private void PropertyChanaged(object sender, PropertyChangedEventArgs e)
		{
			if (sourcePropertyNames.Any(name => name == e.PropertyName))
			{
				if (isNotifyAll)
				{
					targetViewModel.NotifyAll();
				}
				else
				{
					targetPropertyNames.ForEach(name => targetViewModel.Notify(name));
				}
			}

		}


		public void Notify(params string[] propertyNames)
		{
			targetPropertyNames = propertyNames;
		}


		public void NotifyAll()
		{
			isNotifyAll = true;
		}
	}
}