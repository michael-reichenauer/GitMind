using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;


namespace GitMind.Utils.UI
{
	internal class ViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		private readonly Dictionary<string, Property> properties = new Dictionary<string, Property>();
		private readonly Dictionary<string, ICommand> commands = new Dictionary<string, ICommand>();


		internal void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}


		protected Property<T> Property<T>([CallerMemberName] string memberName = "")
		{
			Property property;
			if (!properties.TryGetValue(memberName, out property))
			{

				property = new Property<T>(memberName, this);
				properties[memberName] = property;
			}

			return (Property<T>)property;
		}


		protected BusyIndicator BusyIndicator([CallerMemberName] string memberName = "")
		{
			Property property;
			if (!properties.TryGetValue(memberName, out property))
			{

				property = new BusyIndicator(memberName, this);
				properties[memberName] = property;
			}

			return (BusyIndicator)property;
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
	}
}