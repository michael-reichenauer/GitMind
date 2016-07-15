using System.Windows;
using System.Windows.Controls;


namespace GitMind.Utils.UI
{
	public class HideableGridSplitter : GridSplitter
	{
		private GridLength height = new GridLength(150);

		public HideableGridSplitter()
		{
			this.IsVisibleChanged += HideableGridSplitter_IsVisibleChanged;
		}

		void HideableGridSplitter_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			Grid parent = base.Parent as Grid;
			if (parent == null)
			{
				return;
			}

			int rowIndex = Grid.GetRow(this);
			if (rowIndex + 1 >= parent.RowDefinitions.Count)
			{
				return;
			}

			var lastRow = parent.RowDefinitions[rowIndex + 1];

			if (this.Visibility == Visibility.Visible)
			{
				lastRow.Height = height;
			}
			else
			{
				height = lastRow.Height;
				lastRow.Height = new GridLength(0);
			}
		}
	}
}