using System;
using System.Threading.Tasks;
using System.Windows.Input;


namespace GitMind.Utils.UI
{
	public class Command : ICommand
	{
		private readonly Command<object> command;


		public Command(Action executeMethod)
		{
			command = new Command<object>(_ => executeMethod());
		}

		public Command(Action executeMethod, Func<bool> canExecuteMethod)
		{
			command = new Command<object>((object _) => executeMethod(), _ => canExecuteMethod());
		}

		public Command(Func<Task> executeMethodAsync)
		{
			command = new Command<object>(_ => executeMethodAsync());
		}

		public Command(Func<Task> executeMethodAsync, Func<bool> canExecuteMethod)
		{
			command = new Command<object>(_ => executeMethodAsync(), _ => canExecuteMethod());

		}

		public event EventHandler CanExecuteChanged
		{
			add { command.CanExecuteChanged += value; }
			remove { command.CanExecuteChanged -= value; }
		}

		bool ICommand.CanExecute(object parameter)
		{
			return CanExecute();
		}


		void ICommand.Execute(object parameter)
		{
			Execute();
		}


		public bool CanExecute()
		{
			return command.CanExecute(null);
		}


		public void Execute()
		{
			command.Execute(null);
		}


		public Task ExecuteAsync()
		{
			return command.ExecuteAsync(null);
		}


		public void RaiseCanExecuteChanaged()
		{
			command.RaiseCanExecuteChanaged();
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

	
		public Command With(Func<T> parameterFunc)
		{
			Command cmd = new Command(() => Execute(parameterFunc()), () => CanExecute(parameterFunc()));
			CanExecuteChanged += (s, e) => cmd.RaiseCanExecuteChanaged();

			return cmd;
		}


		// NOTE: Should use weak event if command instance is longer than UI object
		public event EventHandler CanExecuteChanged;

		public bool IsCompleted { get; private set; }

		public bool IsNotCompleted => !IsCompleted;


		public bool CanExecute(T parameter)
		{
			if (canExecuteMethod != null)
			{
				return canExecuteMethod(parameter);
			}

			return canExecute;
		}

		public async void Execute(T parameter)
		{
			await ExecuteAsync(parameter);
		}


		bool ICommand.CanExecute(object parameter)
		{
			return CanExecute((T)parameter);
		}


		void ICommand.Execute(object parameter)
		{
			Execute((T)parameter);
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