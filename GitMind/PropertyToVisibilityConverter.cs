using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GitMind.Utils.UI;


namespace GitMind
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
			bool bValue = false;
			if (value is bool)
			{
				bValue = (bool)value;
			}
			else if (value is Nullable<bool>)
			{
				Nullable<bool> tmp = (Nullable<bool>)value;
				bValue = tmp.HasValue ? tmp.Value : false;
			}
			else if (value is Property<bool>)
			{
				Property<bool> tmp = (Property<bool>)value;
				bValue = tmp.Value;
			}
			else if (value is Property<Nullable<bool>>)
			{
				Property<Nullable<bool>> tmp = (Property<Nullable<bool>>)value;
				bValue = tmp.Value.HasValue ? tmp.Value.Value : false;
			}

			return (bValue) ? Visibility.Visible : Visibility.Collapsed;
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