using System;
using System.Collections.Generic;
using System.Windows.Media;
using GitMind.GitModel;


namespace GitMind.RepositoryViews
{
	internal class BrushService : IBrushService
	{
		private static readonly SolidColorBrush MasterBranchBrush = BrushFromHex("#E540FF");
		private static readonly SolidColorBrush MultiBranchBrush = Brushes.White;
		private static readonly SolidColorBrush LighterBaseBrush = BrushFromHex("#FFFFFFFF");

		private readonly List<Brush> brushes = new List<Brush>();
		private readonly List<Brush> darkBrushes = new List<Brush>();
		private readonly List<Brush> lighterBrushes = new List<Brush>();

		public SolidColorBrush SubjectBrush { get; } = BrushFromHex("#D4D4D4");
		public SolidColorBrush LocalAheadBrush { get; } = Brushes.LightGreen;
		public SolidColorBrush RemoteAheadBrush { get; } = BrushFromHex("#BBBBFB");

		public static SolidColorBrush ConflictBrush { get; } = BrushFromHex("#FCB9B6");
		public static Brush MergeBrush { get; } = Brushes.Gold;

		public SolidColorBrush UnCommittedBrush { get; } = Brushes.BurlyWood;
		public static SolidColorBrush BranchTipBrush { get; } = Brushes.Aqua;

		public static SolidColorBrush TagBrush { get; } = BrushFromHex("#42C650");
		public static SolidColorBrush TextBrush { get; } = BrushFromHex("#D4D4D4");
		public static SolidColorBrush TicketBrush { get; } = BrushFromHex("#F25B54");
		public static Brush DimBrush { get; } = Brushes.DimGray;


		public static readonly SolidColorBrush HoverBrushColor = BrushFromHex("#996495ED");

		public BrushService()
		{
			InitBrushes();
		}


		public Brush GetBranchBrush(Branch branch)
		{
			if (branch.Name == "master")
			{
				return MasterBranchBrush;
			}

			if (branch.IsMultiBranch)
			{
				return MultiBranchBrush;
			}

			return GetBrush(branch.Name);
		}


		public Brush GetDarkerBrush(Brush brush)
		{
			int index = brushes.IndexOf(brush);

			return darkBrushes[index];
		}


		public Brush GetLighterBrush(Brush brush)
		{
			int index = brushes.IndexOf(brush);

			return lighterBrushes[index];
		}


		private Brush GetBrush(string name)
		{
			int branchBrushId = Math.Abs(name.GetHashCode()) % (brushes.Count - 2);
			return brushes[branchBrushId];
		}


		private void InitBrushes()
		{
			//for (int i = 0; i < 100; i += 4)
			//{
			//	Color colorFromHsl = ColorFromHSL((double)i / 100, 0.85, 0.57);
			//	SolidColorBrush brush = new SolidColorBrush(colorFromHsl);

			//	SolidColorBrush darkerBrush = DarkBrush(brush);
			//	SolidColorBrush lighterBrush = LightBrush(brush);

			//	brushes.Add(brush);
			//	darkBrushes.Add(darkerBrush);
			//	lighterBrushes.Add(lighterBrush);
			//}


			foreach (Color color in _kellysMaxContrastSet)
			{
				SolidColorBrush brush = new SolidColorBrush(color);

				SolidColorBrush darkerBrush = DarkBrush(brush);
				SolidColorBrush lighterBrush = LightBrush(brush);

				brushes.Add(brush);
				darkBrushes.Add(darkerBrush);
				lighterBrushes.Add(lighterBrush);
			}


			SolidColorBrush darker = DarkBrush(MasterBranchBrush);
			SolidColorBrush lighter = LightBrush(MasterBranchBrush);

			brushes.Add(MasterBranchBrush);
			darkBrushes.Add(darker);
			lighterBrushes.Add(lighter);

			darker = DarkBrush(MultiBranchBrush);
			lighter = LightBrush(MultiBranchBrush);

			brushes.Add(MultiBranchBrush);
			darkBrushes.Add(darker);
			lighterBrushes.Add(lighter);
		}



		private Color InterpolateColors(Color color1, Color color2, float percentage)
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


		private SolidColorBrush DarkBrush(SolidColorBrush brush)
		{
			SolidColorBrush darkerBrush = new SolidColorBrush(brush.Color);
			darkerBrush.Color = InterpolateColors(brush.Color, Brushes.Black.Color, 0.7f);
			return darkerBrush;
		}


		private SolidColorBrush LightBrush(SolidColorBrush brush)
		{
			SolidColorBrush lighterBrush = new SolidColorBrush(brush.Color);
			lighterBrush.Color = InterpolateColors(brush.Color, LighterBaseBrush.Color, 0.2f);
			return lighterBrush;
		}


		private static SolidColorBrush BrushFromHex(string hexText)
		{
			return (SolidColorBrush)new BrushConverter().ConvertFrom(hexText);
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

		private static double GetColorComponent(double temp1, double temp2, double temp3)
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

		private static readonly IReadOnlyList<Color> _kellysMaxContrastSet = new List<Color>
		{
			UIntToColor(0xFFFFB300), //Vivid Yellow
			UIntToColor(0xFFA12B8E), //Strong Purple
			UIntToColor(0xFFFF6800), //Vivid Orange
			UIntToColor(0xFF6892C0), //Very Light Blue
			UIntToColor(0xFFDF334E), //Vivid Red
			UIntToColor(0xFFCEA262), //Grayish Yellow
			UIntToColor(0xFFAD7E62), //Medium Gray

			//The following will not be good for people with defective color vision
			UIntToColor(0xFF0FA94E), //Vivid Green
			UIntToColor(0xFFF6768E), //Strong Purplish Pink
			UIntToColor(0xFF05588E), //Strong Blue
			UIntToColor(0xFFFF7A5C), //Strong Yellowish Pink
			UIntToColor(0xFF6D568D), //Strong Violet
			UIntToColor(0xFFFF8E00), //Vivid Orange Yellow
			UIntToColor(0xFFB04B6A), //Strong Purplish Red
			UIntToColor(0xFFF4C800), //Vivid Greenish Yellow
			UIntToColor(0xFFA5574F), //Strong Reddish Brown
			UIntToColor(0xFF93AA00), //Vivid Yellowish Green
			UIntToColor(0xFF9C5E2C), //Deep Yellowish Brown
			UIntToColor(0xFFF13A13), //Vivid Reddish Orange
			UIntToColor(0xFF526931), //Dark Olive Green
		};

		private static Color UIntToColor(uint color)
		{
			var a = (byte)(color >> 24);
			var r = (byte)(color >> 16);
			var g = (byte)(color >> 8);
			var b = (byte)(color >> 0);
			return Color.FromArgb(a, r, g, b);
		}
	}
}