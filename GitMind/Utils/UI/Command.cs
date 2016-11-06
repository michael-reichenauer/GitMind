using System;
using System.Threading.Tasks;
using System.Windows.Input;


namespace GitMind.Utils.UI
{
	public class Command : ICommand
	{
		private Command<object> command;


		public Command(Action executeMethod, string memberName)
		{
			SetCommand(executeMethod, memberName);
		}

		public Command(Action executeMethod, Func<bool> canExecuteMethod, string memberName)
		{
			SetCommand(executeMethod, canExecuteMethod, memberName);
		}

		public Command(Func<Task> executeMethodAsync, string memberName)
		{
			SetCommand(executeMethodAsync,  memberName);
		}

		public Command(Func<Task> executeMethodAsync, Func<bool> canExecuteMethod, string memberName)
		{
			SetCommand(executeMethodAsync, canExecuteMethod, memberName);
		}

		protected Command()
		{
			SetCommand(RunAsync, CanRun, GetType().FullName);
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


		private void SetCommand(Action executeMethod, string memberName)
		{
			command = new Command<object>(_ => executeMethod(), memberName);
		}

		private void SetCommand(Action executeMethod, Func<bool> canExecuteMethod, string memberName)
		{
			command = new Command<object>((object _) => executeMethod(), _ => canExecuteMethod(), memberName);
		}

		private void SetCommand(Func<Task> executeMethodAsync, string memberName)
		{
			command = new Command<object>(_ => executeMethodAsync(), memberName);
		}

		private void SetCommand(Func<Task> executeMethodAsync, Func<bool> canExecuteMethod, string memberName)
		{
			command = new Command<object>(_ => executeMethodAsync(), _ => canExecuteMethod(), memberName);
		}


		protected virtual Task RunAsync()
		{
			return Task.CompletedTask;
		}

		protected virtual bool CanRun()
		{
			return true;
		}
	}


	public class Command<T> : ICommand
	{
		private readonly Action<T> executeMethod;
		private string memberName;
		private Func<T, Task> executeMethodAsync;
		private Func<T, bool> canExecuteMethod;
		private bool canExecute = true;


		protected Command()
		{
			SetCommand(RunAsync, CanRun, GetType().FullName);
		}


		private void SetCommand(Func<T, Task> methodAsync, Func<T, bool> canRun, string name)
		{
			Asserter.NotNull(methodAsync);
			Asserter.NotNull(canExecute);

			executeMethodAsync = methodAsync;
			canExecuteMethod = canRun;
			memberName = name;
		}

		private void SetCommand(Func<T, Task> methodAsync, string name)
		{
			Asserter.NotNull(methodAsync);

			executeMethodAsync = methodAsync;
			memberName = name;
		}

		public Command(Action<T> executeMethod, string memberName)
		{
			Asserter.NotNull(executeMethod);

			this.executeMethod = executeMethod;
			this.memberName = memberName;
		}

		public Command(Action<T> executeMethod, Func<T, bool> canExecuteMethod, string memberName)
		{
			Asserter.NotNull(executeMethod);
			Asserter.NotNull(canExecuteMethod);

			this.executeMethod = executeMethod;
			this.canExecuteMethod = canExecuteMethod;
			this.memberName = memberName;
		}


		public Command(Func<T, Task> executeMethodAsync, string memberName)
		{
			SetCommand(executeMethodAsync, memberName);
		}


		public Command(
			Func<T, Task> executeMethodAsync, Func<T, bool> canExecuteMethod, string memberName)
		{
			Asserter.NotNull(executeMethodAsync);
			Asserter.NotNull(canExecuteMethod);

			this.executeMethodAsync = executeMethodAsync;
			this.canExecuteMethod = canExecuteMethod;
			this.memberName = memberName;
		}

	
		public Command With(Func<T> parameterFunc)
		{
			Command cmd = new Command(() => Execute(parameterFunc()), () => CanExecute(parameterFunc()), memberName);
			//CanExecuteChanged += (s, e) => cmd.RaiseCanExecuteChanaged();

			return cmd;
		}


		// NOTE: Should use weak event if command instance is longer than UI object
		//public event EventHandler CanExecuteChanged;

		public event EventHandler CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

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
			CommandManager.InvalidateRequerySuggested();
		}


		public async Task ExecuteAsync(T parameter)
		{
			try
			{
				IsCompleted = false;
				canExecute = false;
				RaiseCanExecuteChanaged();
				Log.Usage(memberName);

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


		protected virtual Task RunAsync(T parameter)
		{
			return Task.CompletedTask;
		}

		protected virtual bool CanRun(T parameter)
		{
			return true;
		}
	}
}