// ZoomableCanvas: From Kael Rowan's blog (http://blogs.msdn.com/kaelr)
// VirtualPanel: http://blogs.msdn.com/b/kaelr/archive/2009/04/27/virtualpanel.aspx
// PriorityQuadTree: http://blogs.msdn.com/b/kaelr/archive/2009/05/21/priorityquadtree.aspx
// MathExtensions: http://blogs.msdn.com/b/kaelr/archive/2008/05/12/mathextensions.aspx
// LinkedListExtensions: 
// http://blogs.msdn.com/b/kaelr/archive/2010/04/09/linkedlist-findnext-and-findprevious.aspx

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows;
using System.Windows.Data;


namespace GitMind.Utils.UI.VirtualCanvas
{
	/// <summary>
	/// Represents the converter that performs simple arithmetic operations on numeric values. 
	/// </summary>
	public sealed class ArithmeticConverter : IValueConverter, IMultiValueConverter
	{
		/// <summary>
		/// Gets the default instance of an arithmetic converter.
		/// </summary>
		/// <value>
		/// A shared instance of an <see cref="ArithmeticConverter"/>.
		/// </value>
		[SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly ArithmeticConverter Default = new ArithmeticConverter();

		/// <summary>
		/// Applies a unary arithmetic operator to a numeric value and returns the result.
		/// </summary>
		/// <param name="value">Input value that is convertable via 
		/// <see cref="System.Convert.ToDouble"/>.</param>
		/// <param name="targetType">Ignored; the target type is always <see cref="Double"/>.</param>
		/// <param name="parameter">The unary arithmetic operator(s) to apply to the value.  Supported 
		/// operators are + (Math.Abs) and - (Negate).</param>
		/// <param name="culture">An <see cref="IFormatProvider"/> interface implementation that 
		/// supplies culture-specific formatting information.</param>
		/// <returns>A <see cref="Double"/> that is the result of transitively applying the operations 
		/// in the <paramref name="parameter"/> to the <paramref name="value"/>.</returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == DependencyProperty.UnsetValue)
			{
				return DependencyProperty.UnsetValue;
			}

			var operations = parameter as string ?? "";

			var result = System.Convert.ToDouble(value, culture);
			for (int o = 0; o < operations.Length; o++)
			{
				result = Interpret(operations[o], result);
			}
			return result;
		}

		/// <summary>
		/// Transitively applies a binary arithmetic operator to a set of numeric values and returns 
		/// the result. 
		/// </summary>
		/// <param name="values">Input values that are convertable via 
		/// <see cref="System.Convert.ToDouble"/>.</param>
		/// <param name="targetType">Ignored; the target type is always <see cref="Double"/>.</param>
		/// <param name="parameter">The binary arithmetic operator(s) to apply to the values.  
		/// Supported operators are +, -, *, /, %, and ^.  The number of operators must be exactly one 
		/// fewer than the number of values.</param>
		/// <param name="culture">An <see cref="IFormatProvider"/> interface implementation that 
		/// supplies culture-specific formatting information.</param>
		/// <returns>A <see cref="Double"/> that is the result of transitively applying the operations 
		/// in the <paramref name="parameter"/> to the <paramref name="values"/>.</returns>
		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		[SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly")]
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values == null)
			{
				throw new ArgumentNullException("values");
			}

			if (Array.IndexOf(values, DependencyProperty.UnsetValue) >= 0)
			{
				return DependencyProperty.UnsetValue;
			}

			var operations = parameter as string ?? "";

			if (operations.Length != values.Length - 1)
			{
				throw new ArgumentException(
					"The number of arithmetic operators (" + operations.Length +
					") does not match the number of values (" + values.Length + ").",
					"parameter");
			}

			var result = System.Convert.ToDouble(values[0], culture);
			for (int o = 0; o < operations.Length; o++)
			{
				var operand = System.Convert.ToDouble(values[o + 1], culture);
				result = Interpret(operations[o], result, operand);
			}
			return result;
		}

		/// <summary>
		/// Performs the given unary <paramref name="operation"/> on a single operand.
		/// </summary>
		/// <param name="operation">The operation to perform.</param>
		/// <param name="operand">The operand.</param>
		/// <returns>The result of applying the <paramref name="operation"/> to the 
		/// <paramref name="operand"/>.</returns>
		private static double Interpret(char operation, double operand)
		{
			switch (operation)
			{
				case '+': return Math.Abs(operand);
				case '-': return -operand;
				default: throw new ArgumentException("Unknown unary operator: " + operation, "operation");
			}
		}

		/// <summary>
		/// Performs the given binary <paramref name="operation"/> on two operands.
		/// </summary>
		/// <param name="operation">The operation to perform.</param>
		/// <param name="operand1">The first operand.</param>
		/// <param name="operand2">The second operand.</param>
		/// <returns>The result of <paramref name="operand1"/> <paramref name="operation"/> <paramref name="operand2"/>.</returns>
		private static double Interpret(char operation, double operand1, double operand2)
		{
			switch (operation)
			{
				case '+': return operand1 + operand2;
				case '-': return operand1 - operand2;
				case '*': return operand1 * operand2;
				case '/': return operand1 / operand2;
				case '%': return operand1 % operand2;
				case '^': return Math.Pow(operand1, operand2);
				default: throw new ArgumentException("Unknown binary operator: " + operation, "operation");
			}
		}

		/// <summary>
		/// Not supported.
		/// </summary>
		object IValueConverter.ConvertBack(
			object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Not supported.
		/// </summary>
		object[] IMultiValueConverter.ConvertBack(
			object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}