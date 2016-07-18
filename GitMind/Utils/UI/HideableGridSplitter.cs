using System.Windows;
using System.Windows.Controls;


namespace GitMind.Utils.UI
{
	/// <summary>
	/// Grid splitter, which can toggle visibility of grid row after a the grid splitter
	/// </summary>
	public class HideableGridSplitter : GridSplitter
	{
		private static readonly GridLength CollapsedRow = new GridLength(0);
		private GridLength height;
		private bool isInitialized;


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
				// No row after this splitter
				return;
			}

			// Get the row after the splitter to hide or show
			RowDefinition lastRow = parent.RowDefinitions[rowIndex + 1];

			if (!isInitialized)
			{
				// Store the initial row height 
				height = lastRow.Height;
				isInitialized = true;
			}

			if (Visibility == Visibility.Visible)
			{
				lastRow.Height = height;
			}
			else
			{
				height = lastRow.Height;
				lastRow.Height = CollapsedRow;
			}
		}
	}
}