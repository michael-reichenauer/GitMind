using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GitMind.Features.Commits
{
	/// <summary>
	/// Interaction logic for CommitDialog.xaml
	/// </summary>
	public partial class CommitDialog : Window
	{
		public CommitDialog()
		{
			InitializeComponent();
		}


		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}


		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}


		private void Exscape_Clicked(object sender, EventArgs e)
		{
			DialogResult = false;
		}
	}
}
