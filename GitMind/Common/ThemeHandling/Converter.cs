using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GitMind.Common.ThemeHandling
{
	internal static class Converter
	{
		public static SolidColorBrush BrushFromHex(string hexText)
		{
			return (SolidColorBrush)new BrushConverter().ConvertFrom(hexText);
		}


		public static string HexFromBrush(Brush brush)
		{
			return (string)new BrushConverter().ConvertTo(brush, typeof(string));
		}


		public static Color ColorFromHSL(double h, double s, double l)
		{
			double r = 0, g = 0, b = 0;
			if (l != 0)
			{
				if (s == 0)
					r = g = b = l;
				else
				{
					double temp2;
					if (l < 0.5)
						temp2 = l * (1.0 + s);
					else
						temp2 = l + s - (l * s);

					double temp1 = 2.0 * l - temp2;

					r = GetColorComponent(temp1, temp2, h + 1.0 / 3.0);
					g = GetColorComponent(temp1, temp2, h);
					b = GetColorComponent(temp1, temp2, h - 1.0 / 3.0);
				}
			}
			Color colorFromHsl = Color.FromArgb(255, (byte)(255 * r), (byte)(255 * g), (byte)(255 * b));
			return colorFromHsl;

		}

		public static double GetColorComponent(double temp1, double temp2, double temp3)
		{
			if (temp3 < 0.0)
				temp3 += 1.0;
			else if (temp3 > 1.0)
				temp3 -= 1.0;

			if (temp3 < 1.0 / 6.0)
				return temp1 + (temp2 - temp1) * 6.0 * temp3;
			else if (temp3 < 0.5)
				return temp2;
			else if (temp3 < 2.0 / 3.0)
				return temp1 + ((temp2 - temp1) * ((2.0 / 3.0) - temp3) * 6.0);
			else
				return temp1;
		}


		public static Color InterpolateColors(Color color1, Color color2, float percentage)
		{
			double a1 = color1.A / 255.0;
			double r1 = color1.R / 255.0;
			double g1 = color1.G / 255.0;
			double b1 = color1.B / 255.0;

			double a2 = color2.A / 255.0;
			double r2 = color2.R / 255.0;
			double g2 = color2.G / 255.0;
			double b2 = color2.B / 255.0;

			byte a3 = Convert.ToByte((a1 + (a2 - a1) * percentage) * 255);
			byte r3 = Convert.ToByte((r1 + (r2 - r1) * percentage) * 255);
			byte g3 = Convert.ToByte((g1 + (g2 - g1) * percentage) * 255);
			byte b3 = Convert.ToByte((b1 + (b2 - b1) * percentage) * 255);

			return Color.FromArgb(a3, r3, g3, b3);
		}
	}
}
