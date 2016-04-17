using System;
using System.Threading.Tasks;
using System.Windows.Input;


namespace GitMind.Utils.UI
{
	public class Command : Command<object>
	{
		public Command(Action executeMethod)
			: base(_ => executeMethod())
		{
		}

		public Command(Action executeMethod, Func<bool> canExecuteMethod)
			: base((object _) => executeMethod(), _ => canExecuteMethod())
		{
		}

		public Command(Func<Task> executeMethodAsync)
			: base(_ => executeMethodAsync())
		{
		}

		public Command(Func<Task> executeMethodAsync, Func<bool> canExecuteMethod)
			: base(_ => executeMethodAsync(), _ => canExecuteMethod())
		{
		}
	}


	public class Command<T> : ICommand
	{
		private readonly Action<T> executeMethod;
		private readonly Func<T, Task> executeMethodAsync;
		private readonly Func<T, bool> canExecuteMethod;
		private bool canExecute = true;

		public Command(Action<T> executeMethod)
		{
			Asserter.NotNull(executeMethod);

			this.executeMethod = executeMethod;
		}

		public Command(Action<T> executeMethod, Func<T, bool> canExecuteMethod)
		{
			Asserter.NotNull(executeMethod);
			Asserter.NotNull(canExecuteMethod);

			this.executeMethod = executeMethod;
			this.canExecuteMethod = canExecuteMethod;
		}


		public Command(Func<T, Task> executeMethodAsync)
		{
			Asserter.NotNull(executeMethodAsync);

			this.executeMethodAsync = executeMethodAsync;
		}


		public Command(Func<T, Task> executeMethodAsync, Func<T, bool> canExecuteMethod)
		{
			Asserter.NotNull(executeMethodAsync);
			Asserter.NotNull(canExecuteMethod);

			this.executeMethodAsync = executeMethodAsync;
			this.canExecuteMethod = canExecuteMethod;
		}



		// NOTE: Should use weak event if command instance is longer than UI object
		public event EventHandler CanExecuteChanged;

		public bool IsCompleted { get; private set; }

		public bool IsNotCompleted => !IsCompleted;


		bool ICommand.CanExecute(object parameter)
		{
			if (canExecuteMethod != null)
			{
				return canExecuteMethod((T)parameter);
			}

			return canExecute;
		}


		async void ICommand.Execute(object parameter)
		{
			await ExecuteAsync((T)parameter);
		}


		public void RaiseCanExecuteChanaged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}


		public async Task ExecuteAsync(T parameter)
		{
			try
			{
				IsCompleted = false;
				canExecute = false;
				RaiseCanExecuteChanaged();

				if (executeMethod != null)
				{
					// Sync command
					executeMethod(parameter);
				}
				else
				{
					// Async command
					await executeMethodAsync(parameter);
				}			
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Asserter.FailFast($"Unhandled command exception {e}");
			}
			finally
			{
				IsCompleted = false;
				canExecute = true;
				RaiseCanExecuteChanaged();
			}
		}
	}
}