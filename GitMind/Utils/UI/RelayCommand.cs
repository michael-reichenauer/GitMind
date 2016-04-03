using System;
using System.Windows.Input;


namespace GitMind.Utils.UI
{
	public class RelayCommand : ICommand
	{
		private readonly Action executeMethod;
		private readonly Func<bool> canExecuteMethod;


		public RelayCommand(Action executeMethod)
		{
			this.executeMethod = executeMethod;
		}


		public RelayCommand(Action executeMethod, Func<bool> canExecuteMethod)
		{
			this.executeMethod = executeMethod;
			this.canExecuteMethod = canExecuteMethod;
		}


		// NOTE: Should use weak event if command instance is longer than UI object
		public event EventHandler CanExecuteChanged;

		public void RaiseCanExecuteChanaged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}

		bool ICommand.CanExecute(object parameter)
		{
			return canExecuteMethod == null || canExecuteMethod();
		}

		void ICommand.Execute(object parameter)
		{
			if (executeMethod != null)
			{
				executeMethod();
			}			
		}
	}


	public class RelayCommand <T>: ICommand
	{
		private readonly Action<T> executeMethod;
		private readonly Func<T, bool> canExecuteMethod;


		public RelayCommand(Action<T> executeMethod)
		{
			this.executeMethod = executeMethod;
		}


		public RelayCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod)
		{
			this.executeMethod = executeMethod;
			this.canExecuteMethod = canExecuteMethod;
		}

		// NOTE: Should use weak event if command instance is longer than UI object
		public event EventHandler CanExecuteChanged;

		public void RaiseCanExecuteChanaged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}

		bool ICommand.CanExecute(object parameter)
		{
			return canExecuteMethod == null || canExecuteMethod((T)parameter);
		}


		void ICommand.Execute(object parameter)
		{
			if (executeMethod != null)
			{
				executeMethod((T)parameter);
			}
		}
	}
}