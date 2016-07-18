namespace GitMind.Utils.UI
{
	internal class PropertySetter
	{
		private readonly ViewModel viewModel;


		public PropertySetter(bool isSet, ViewModel viewModel)
		{
			this.viewModel = viewModel;
			IsSet = isSet;
		}


		public bool IsSet { get; }

		public void Notify(params string[] otherPropertyNames)
		{
			if (IsSet)
			{
				viewModel.Notify(otherPropertyNames);
			}
		}

		public void NotifyAll()
		{
			if (IsSet)
			{
				viewModel.NotifyAll();
			}
		}
	}
}