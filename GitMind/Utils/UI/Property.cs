using System.Collections.Generic;


namespace GitMind.Utils.UI
{
	internal class Property
	{		
	}

	internal class Property<T> : Property
	{
		List<string> otherProperties;

		private readonly string propertyName;
		private readonly ViewModel viewModel;
		private T propertyValue;

		public Property(string propertyName, ViewModel viewModel)
		{
			this.propertyName = propertyName;
			this.viewModel = viewModel;
		}


		public T Value
		{
			get { return propertyValue; }
			set
			{
				// Investigate if we can avoid assigning same value ####
				propertyValue = value;
				viewModel.OnPropertyChanged(propertyName);
	
				// Trigger related properties (if specified)			
				otherProperties?.ForEach(property => viewModel.OnPropertyChanged(propertyName));				
			}
		}

		public static implicit operator T(Property<T> propertyInstance) => propertyInstance.Value;

		public void WhenSetNotify(string name)
		{
			if (otherProperties == null)
			{
				otherProperties = new List<string>();
			}

			otherProperties.Add(name);
		}


		public override string ToString() => propertyValue?.ToString();
	}
}