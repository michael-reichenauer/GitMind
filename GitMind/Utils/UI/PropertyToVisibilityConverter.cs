using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;


namespace GitMind.Utils.UI
{
	/// <summary>
	/// Convert between boolean and visibility
	/// </summary>
	[Localizability(LocalizationCategory.NeverLocalize)]
	public sealed class PropertyToVisibilityConverter : IValueConverter
	{
		/// <summary> 
		/// Convert bool or Nullable&lt;bool&gt; to Visibility
		/// </summary> 
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool isVisible = false;
			if (value is bool)
			{
				isVisible = (bool)value;
			}
			else if (value is Nullable<bool>)
			{
				Nullable<bool> typedValue = (Nullable<bool>)value;
				isVisible = typedValue ?? false;
			}
			else if (value is int)
			{
				isVisible = ((int)value) != 0;
			}
			else if (value is string)
			{
				isVisible = !string.IsNullOrEmpty((string)value);
			}

			else if (value is IEnumerable)
			{
				isVisible = IsEmpty((IEnumerable)value);
			}
			else 
			{
				isVisible = value != null;
			}

			return isVisible ? Visibility.Visible : Visibility.Collapsed;
		}


		private bool IsEmpty(IEnumerable enumerable)
		{
			foreach (object obj in enumerable)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Convert Visibility to boolean 
		/// </summary>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Visibility)
			{
				return (Visibility)value == Visibility.Visible;
			}
			else
			{
				return false;
			}
		}
	}
}