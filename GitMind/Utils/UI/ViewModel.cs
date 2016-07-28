using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;


namespace GitMind.Utils.UI
{
	internal class ViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		private readonly Dictionary<string, Property> properties = new Dictionary<string, Property>();
		private readonly Dictionary<string, ICommand> commands = new Dictionary<string, ICommand>();
		private readonly Dictionary<string, BusyIndicator> busyIndicators =
			new Dictionary<string, BusyIndicator>();

		private IList<string> allPropertyNames = null;

		internal void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}


		protected WhenSetter WhenSet(ViewModel viewModel, params string[] sourcePropertyName)
		{
			return new WhenSetter(this, viewModel, sourcePropertyName);
		}


		internal void Notify(params string[] otherPropertyNames)
		{
			foreach (string otherPropertyName in otherPropertyNames)
			{
				OnPropertyChanged(otherPropertyName);
			}
		}


		internal void NotifyAll()
		{
			if (allPropertyNames == null)
			{
				allPropertyNames = this.GetType()
					.GetProperties()
					.Where(pi => pi.GetGetMethod() != null)
					.Select(pi => pi.Name)
					.ToList();
			}

			foreach (string propertyName in allPropertyNames)
			{
				OnPropertyChanged(propertyName);
			}
		}

		protected Property Get([CallerMemberName] string memberName = "")
		{
			Property property;
			if (!properties.TryGetValue(memberName, out property))
			{
				property = new Property(memberName, this);
				properties[memberName] = property;
			}

			return property;
		}


		protected T Get<T>([CallerMemberName] string memberName = "")
		{
			Property property = Get(memberName);
			if (property.Value == null)
			{
				return default(T);
			}

			return (T)property.Value;
		}


		protected PropertySetter Set<T>(T value, [CallerMemberName] string memberName = "")
		{
			Property property = Get(memberName);
			return property.Set(value);
		}


		protected BusyIndicator BusyIndicator([CallerMemberName] string memberName = "")
		{
			BusyIndicator busyIndicator;
			if (!busyIndicators.TryGetValue(memberName, out busyIndicator))
			{

				busyIndicator = new BusyIndicator(memberName, this);
				busyIndicators[memberName] = busyIndicator;
			}

			return busyIndicator;
		}


		protected Command<T> Command<T>(
			Action<T> executeMethod, [CallerMemberName] string memberName = "")
		{
			ICommand command;
			if (!commands.TryGetValue(memberName, out command))
			{

				command = new Command<T>(executeMethod);
				commands[memberName] = command;
			}

			return (Command<T>)command;
		}

		protected Command<T> Command<T>(
			Action<T> executeMethod, Func<T, bool> canExecuteMethod, [CallerMemberName] string memberName = "")
		{
			ICommand command;
			if (!commands.TryGetValue(memberName, out command))
			{

				command = new Command<T>(executeMethod, canExecuteMethod);
				commands[memberName] = command;
			}

			return (Command<T>)command;
		}


		protected Command Command(Action executeMethod, [CallerMemberName] string memberName = "")
		{
			ICommand command;
			if (!commands.TryGetValue(memberName, out command))
			{

				command = new Command(executeMethod);
				commands[memberName] = command;
			}

			return (Command)command;
		}


		protected Command Command(
			Action executeMethod, Func<bool> canExecuteMethod, [CallerMemberName] string memberName = "")
		{
			ICommand command;
			if (!commands.TryGetValue(memberName, out command))
			{

				command = new Command(executeMethod, canExecuteMethod);
				commands[memberName] = command;
			}

			return (Command)command;
		}



		protected Command AsyncCommand(
			Func<Task> executeMethodAsync, [CallerMemberName] string memberName = "")
		{
			ICommand command;
			if (!commands.TryGetValue(memberName, out command))
			{

				command = new Command(executeMethodAsync);
				commands[memberName] = command;
			}

			return (Command)command;
		}


		protected Command<T> AsyncCommand<T>(
			Func<T, Task> executeMethodAsync, [CallerMemberName] string memberName = "")
		{
			ICommand command;
			if (!commands.TryGetValue(memberName, out command))
			{

				command = new Command<T>(executeMethodAsync);
				commands[memberName] = command;
			}

			return (Command<T>)command;
		}
	}
}