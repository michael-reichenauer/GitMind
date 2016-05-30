using System;
using System.Collections.Generic;
using System.Windows.Media;
using GitMind.DataModel;
using GitMind.DataModel.Old;
using GitMind.GitModel;


namespace GitMind.CommitsHistory
{
	internal class BrushService : IBrushService
	{
		private static readonly SolidColorBrush MasterBranchBrush = Brushes.DarkOrchid;
		private static readonly SolidColorBrush MultiBranchBrush = Brushes.White;

		private readonly Lazy<IList<Brush>> brushes = new Lazy<IList<Brush>>(InitBrushes);

		public SolidColorBrush SubjectBrush { get; } = Brushes.Lavender;

		public SolidColorBrush LocalAheadBrush { get; } = Brushes.LightSkyBlue;

		public SolidColorBrush RemoteAheadBrush { get; } = Brushes.Gray;


		public Brush GetBranchBrush(IBranch branch)
		{
			if (branch.Name == "master")
			{
				return MasterBranchBrush;
			}

			if (branch.IsMultiBranch)
			{
				return MultiBranchBrush;
			}

			int branchBrushId = Math.Abs(branch.Name.GetHashCode()) % brushes.Value.Count;
			return brushes.Value[branchBrushId];
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

			int branchBrushId = Math.Abs(branch.Name.GetHashCode()) % brushes.Value.Count;
			return brushes.Value[branchBrushId];
		}



		public Brush GetDarkerBrush(Brush brush)
		{
			SolidColorBrush solidBrush = brush as SolidColorBrush;
			SolidColorBrush darkerBrush = new SolidColorBrush(solidBrush.Color);
			darkerBrush.Color = InterpolateColors(solidBrush.Color, Brushes.Black.Color, 0.8f);

			return darkerBrush;
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



		private static IList<Brush> InitBrushes()
		{
			List<Brush> brush = new List<Brush>();

			brush.Add(Brushes.Maroon);
			brush.Add(Brushes.CadetBlue);
			brush.Add(Brushes.Crimson);
			brush.Add(Brushes.DeepSkyBlue);
			brush.Add(Brushes.MediumSeaGreen);
			brush.Add(Brushes.IndianRed);
			brush.Add(Brushes.Teal);
			brush.Add(Brushes.Green);
			brush.Add(Brushes.DarkGoldenrod);
			brush.Add(Brushes.Aquamarine);
			brush.Add(Brushes.Aqua);
			brush.Add(Brushes.Bisque);
			brush.Add(Brushes.Coral);
			brush.Add(Brushes.DarkKhaki);
			brush.Add(Brushes.HotPink);
			brush.Add(Brushes.MediumSpringGreen);
			brush.Add(Brushes.Violet);
			brush.Add(Brushes.Tomato);
			brush.Add(Brushes.LightPink);
			brush.Add(Brushes.Fuchsia);
			brush.Add(Brushes.YellowGreen);
			brush.Add(Brushes.DodgerBlue);

			return brush;
		}
	}
}