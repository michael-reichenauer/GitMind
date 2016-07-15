using System;
using System.Collections.Generic;
using System.Windows.Media;
using GitMind.GitModel;


namespace GitMind.CommitsHistory
{
	internal class BrushService : IBrushService
	{  
		//225, 37, 255 Brushes.DarkOrchid
		private static readonly SolidColorBrush MasterBranchBrush =
			(SolidColorBrush)new BrushConverter().ConvertFrom("#E540FF");
		private static readonly SolidColorBrush MultiBranchBrush = Brushes.White;

		private readonly Lazy<IList<Brush>> brushes = new Lazy<IList<Brush>>(InitBrushes2);
		private readonly Lazy<IList<Brush>> darkBrushes = new Lazy<IList<Brush>>(InitDarkBrushes2);




		//public SolidColorBrush SubjectBrush { get; } = Brushes.Lavender;
		public SolidColorBrush SubjectBrush { get; } = Brushes.Lavender;

		//public SolidColorBrush LocalAheadBrush { get; } = Brushes.LightSkyBlue;
		public SolidColorBrush LocalAheadBrush { get; } = Brushes.LightGreen;
		//	(SolidColorBrush)new BrushConverter().ConvertFrom("#BBFBD8");

		//public SolidColorBrush RemoteAheadBrush { get; } = Brushes.Gray;
		public SolidColorBrush RemoteAheadBrush { get; } =
			(SolidColorBrush)new BrushConverter().ConvertFrom("#BBBBFB");

		public SolidColorBrush ConflictBrush { get; } =
			(SolidColorBrush)new BrushConverter().ConvertFrom("#FCB9B6");

		public SolidColorBrush UnCommittedBrush { get; } = Brushes.BurlyWood;
		public SolidColorBrush BranchTipBrush { get; } = Brushes.Aqua;

		// Light orchchid
		//public SolidColorBrush RemoteAheadBrush { get; } =
		//		(SolidColorBrush)new BrushConverter().ConvertFrom("#BBBBFB");



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

			int branchBrushId = Math.Abs(branch.Name.GetHashCode())%brushes.Value.Count;
			return brushes.Value[branchBrushId];
		}


		public Brush GetBranchBrush(string branchName)
		{
			if (branchName == "master")
			{
				return MasterBranchBrush;
			}

			int branchBrushId = Math.Abs(branchName.GetHashCode())%brushes.Value.Count;
			return brushes.Value[branchBrushId];
		}


		public Brush GetDarkerBrush(Brush brush)
		{
			SolidColorBrush solidBrush = brush as SolidColorBrush;
			SolidColorBrush darkerBrush = new SolidColorBrush(solidBrush.Color);
			darkerBrush.Color = InterpolateColors(solidBrush.Color, Brushes.Black.Color, 0.5f);

			return darkerBrush;
		}


		private Color InterpolateColors(Color color1, Color color2, float percentage)
		{
			double a1 = color1.A/255.0;
			double r1 = color1.R/255.0;
			double g1 = color1.G/255.0;
			double b1 = color1.B/255.0;

			double a2 = color2.A/255.0;
			double r2 = color2.R/255.0;
			double g2 = color2.G/255.0;
			double b2 = color2.B/255.0;

			byte a3 = Convert.ToByte((a1 + (a2 - a1)*percentage)*255);
			byte r3 = Convert.ToByte((r1 + (r2 - r1)*percentage)*255);
			byte g3 = Convert.ToByte((g1 + (g2 - g1)*percentage)*255);
			byte b3 = Convert.ToByte((b1 + (b2 - b1)*percentage)*255);

			return Color.FromArgb(a3, r3, g3, b3);
		}



		//private static IList<Brush> InitBrushes()
		//{
		//	List<Brush> brush = new List<Brush>();

		//	brush.Add(Brushes.Maroon);
		//	brush.Add(Brushes.CadetBlue);
		//	brush.Add(Brushes.Crimson);
		//	brush.Add(Brushes.DeepSkyBlue);
		//	brush.Add(Brushes.MediumSeaGreen);
		//	brush.Add(Brushes.IndianRed);
		//	brush.Add(Brushes.Teal);
		//	brush.Add(Brushes.Green);
		//	brush.Add(Brushes.DarkGoldenrod);
		//	brush.Add(Brushes.Aquamarine);
		//	brush.Add(Brushes.Aqua);
		//	brush.Add(Brushes.Bisque);
		//	brush.Add(Brushes.Coral);
		//	brush.Add(Brushes.DarkKhaki);
		//	brush.Add(Brushes.HotPink);
		//	brush.Add(Brushes.MediumSpringGreen);
		//	brush.Add(Brushes.Violet);
		//	brush.Add(Brushes.Tomato);
		//	brush.Add(Brushes.LightPink);
		//	brush.Add(Brushes.Fuchsia);
		//	brush.Add(Brushes.YellowGreen);
		//	brush.Add(Brushes.DodgerBlue);

		//	return brush;
		//}

		private static IList<Brush> InitDarkBrushes2()
		{
			throw new NotImplementedException();
		}


		private static IList<Brush> InitBrushes2()
		{
			List<Brush> brushes = new List<Brush>(); 
			for (int i = 0; i < 100; i += 2)
			{
				Color colorFromHsl = ColorFromHSL((double)i/100, 0.85, 0.57);
				brushes.Add(new SolidColorBrush(colorFromHsl));
			}

			return brushes;
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
	}
}