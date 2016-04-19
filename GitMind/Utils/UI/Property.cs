using System.Windows;
using System.Windows.Media;


namespace GitMind.Utils.UI
{
	internal class Property : IPropertySetter
	{
		private readonly string propertyName;
		private readonly ViewModel viewModel;

		private object propertyValue;

		public Property(string propertyName, ViewModel viewModel)
		{
			this.propertyName = propertyName;
			this.viewModel = viewModel;
		}


		internal object Value
		{
			get { return propertyValue; }
			set
			{
				if (propertyValue != value)
				{
					propertyValue = value;

					viewModel.OnPropertyChanged(propertyName);
				}
			}
		}

		public static implicit operator string(Property instance) => (string)instance.Value;
		public static implicit operator bool(Property instance) => (bool?)instance.Value ?? false;
		public static implicit operator int(Property instance) => (int?)instance.Value ?? 0;
		public static implicit operator double(Property instance) => (double?)instance.Value ?? 0;
		public static implicit operator Rect(Property instance) => (Rect?)instance.Value ?? Rect.Empty;
		public static implicit operator Brush(Property instance) => (Brush)instance.Value;
		

		public void Notify(params string[] otherPropertyNames)
		{
			foreach (string otherPropertyName in otherPropertyNames)
			{
				viewModel.OnPropertyChanged(otherPropertyName);
			}
		}
	}


	//internal class AsyncProperty<T> 
	//{

	//	//private T value;
	//	private Task<T> valueTask;

	//	public Property(string propertyName, ViewModel viewModel)
	//		: base(propertyName, viewModel)
	//	{

	//		valueTask = Task.FromResult(default(T));
	//	}


	//	public static implicit operator T(Property<T> propertyInstance) => propertyInstance.Get();

	//	public T Value => Get();

	//	public T Get()
	//	{
	//		return (valueTask.Status == TaskStatus.RanToCompletion)
	//			? valueTask.Result : default(T);
	//	}

	//	protected override object InternalGet()
	//	{
	//		return Get();
	//	}


	//	public void Set(T propertyValue)
	//	{
	//		// Investigate if we can avoid assigning same value ####
	//		Set(Task.FromResult(propertyValue));
	//	}


	//	public void Set(Task<T> propertyValueTask)
	//	{
	//		valueTask = propertyValueTask;
	//		NotifyChanged();

	//		if (!valueTask.IsCompleted)
	//		{
	//			SetAsync(propertyValueTask).RunInBackground();
	//		}
	//	}


	//	private async Task SetAsync(Task<T> task)
	//	{
	//		try
	//		{
	//			await task;
	//		}
	//		catch (Exception e) when (e.IsNotFatal())
	//		{
	//			// Errors will be handled by the task
	//		}

	//		NotifyChanged();
	//	}


	//	//public T Result => (valueTask.Status == TaskStatus.RanToCompletion)
	//	//	? valueTask.Result : default(T);

	//	public bool IsCompleted => valueTask.IsCompleted;
	//	public bool IsNotCompleted => !valueTask.IsCompleted;
	//	public bool IsSuccessfullyCompleted => valueTask.Status == TaskStatus.RanToCompletion;
	//	public bool IsNotSuccessfullyCompleted => !IsSuccessfullyCompleted;
	//	public bool IsCanceled => valueTask.IsCanceled;
	//	public bool IsFaulted => valueTask.IsFaulted;

	//	// public TaskStatus Status => valueTask.Status;
	//	//public AggregateException Exception => valueTask.Exception;
	//	//public Exception InnerException => Exception?.InnerException;
	//	//public string ErrorMessage => InnerException?.Message;



	//	public void NotifyChanged()
	//	{
	//		viewModel.OnPropertyChanged(propertyName);

	//		// Trigger related properties (if specified using WhenSetAlsoNotify)			
	//		otherProperties?.ForEach(property => viewModel.OnPropertyChanged(propertyName));
	//	}


	//	public void WhenSetAlsoNotify(string otherPropertyName)
	//	{
	//		if (otherProperties == null)
	//		{
	//			otherProperties = new List<string>();
	//		}

	//		otherProperties.Add(otherPropertyName);
	//	}


	//	public override string ToString() => Get()?.ToString() ?? "";

	//}
}