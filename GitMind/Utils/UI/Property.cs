using System;
using System.Collections.Generic;
using System.Threading.Tasks;


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

		//private T value;
		private Task<T> valueTask;

		public Property(string propertyName, ViewModel viewModel)
		{
			this.propertyName = propertyName;
			this.viewModel = viewModel;
			valueTask = Task.FromResult(default(T));
		}


		public static implicit operator T(Property<T> propertyInstance) => propertyInstance.Get();

		public T Value => Get();

		public T Get()
		{
			return (valueTask.Status == TaskStatus.RanToCompletion)
				? valueTask.Result : default(T);
		}


		public void Set(T propertyValue)
		{
			// Investigate if we can avoid assigning same value ####
			Set(Task.FromResult(propertyValue));
		}


		public void Set(Task<T> propertyValueTask)
		{
			valueTask = propertyValueTask;
			NotifyChanged();

			if (!valueTask.IsCompleted)
			{
				SetAsync(propertyValueTask).RunInBackground();
			}
		}


		private async Task SetAsync(Task<T> task)
		{
			try
			{
				await task;
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				// Errors will be handled by the task
			}

			NotifyChanged();
		}


		//public T Result => (valueTask.Status == TaskStatus.RanToCompletion)
		//	? valueTask.Result : default(T);
		
		public bool IsCompleted => valueTask.IsCompleted;
		public bool IsNotCompleted => !valueTask.IsCompleted;
		public bool IsSuccessfullyCompleted => valueTask.Status == TaskStatus.RanToCompletion;
		public bool IsNotSuccessfullyCompleted => !IsSuccessfullyCompleted;
		public bool IsCanceled => valueTask.IsCanceled;
		public bool IsFaulted => valueTask.IsFaulted;

		// public TaskStatus Status => valueTask.Status;
		//public AggregateException Exception => valueTask.Exception;
		//public Exception InnerException => Exception?.InnerException;
		//public string ErrorMessage => InnerException?.Message;



		public void NotifyChanged()
		{
			viewModel.OnPropertyChanged(propertyName);

			// Trigger related properties (if specified using WhenSetAlsoNotify)			
			otherProperties?.ForEach(property => viewModel.OnPropertyChanged(propertyName));
		}


		public void WhenSetAlsoNotify(string otherPropertyName)
		{
			if (otherProperties == null)
			{
				otherProperties = new List<string>();
			}

			otherProperties.Add(otherPropertyName);
		}


		public override string ToString() => Get()?.ToString() ?? "";
	}
}