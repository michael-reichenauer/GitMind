using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using GitMind.MainWindowViews;
using GitMind.Utils;


namespace GitMind.RepositoryViews.Open
{
	/// <summary>
	/// Interaction logic for CloneDialog.xaml
	/// </summary>
	public partial class CloneDialog : Window
	{
		internal CloneDialog(WindowOwner owner)
		{
			InitializeComponent();
			Owner = owner;
		}


		public string UriText => Uri.Text ?? (Uri.SelectedItem as string) ?? "";
		public string FolderText => Folder.Text ?? "";

		public void AddUri(string uri)
		{
			bool isFirst = Uri.Items.Count == 0;
			Uri.Items.Add(uri);
			if (isFirst)
			{
				Uri.SelectedItem = Uri.Items[0];
			}
		}


		public void SetInitialFolder(string path)
		{
			Folder.Text = path + "\\";

			SetFolder((Uri.SelectedItem as string) ?? "");
		}


		private void OKButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;


		private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;


		private void Uri_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SetFolder((Uri.SelectedItem as string) ?? "");
		}


		private void SetFolder(string uri)
		{

			try
			{
				string path = Folder.Text;
				if (!path.EndsWith("\\"))
				{
					path = Path.GetDirectoryName(path) + "\\";
				}


				string[] parts = uri.Split("/\\".ToCharArray());
				if (parts.Length > 1)
				{
					string lastPart = parts.Last();
					if (lastPart.Length > 4 && lastPart.EndsWith(".git", StringComparison.InvariantCultureIgnoreCase))
					{
						lastPart = lastPart.Substring(0, lastPart.Length - 4);
					}

					path = path + lastPart;
				}

				Folder.Text = path;
			}
			catch (Exception ex)
			{
				Log.Warn($"Failed to set path {ex.Message}");
			}
		}


		private void Uri_OnLostFocus(object sender, RoutedEventArgs e)
		{
			SetFolder(Uri.Text ?? (Uri.SelectedItem as string) ?? "");
		}


		private void Browse_OnClick(object sender, RoutedEventArgs e)
		{
			string path = Folder.Text;
			if (path.EndsWith("\\"))
			{
				path = path.Substring(0, path.Length - 1);
			}
			else
			{
				path = Path.GetDirectoryName(path) + "\\";
			}

			FolderBrowserDialog dialog = new FolderBrowserDialog()
			{
				Description = "Select a folder to clone into:",
				ShowNewFolderButton = true,
				//RootFolder = Environment.SpecialFolder.MyComputer
			};

			dialog.SelectedPath = path;


			IWin32Window owner = WindowOwner.AsWin32Window(Owner);
			if (dialog.ShowDialog(owner) != System.Windows.Forms.DialogResult.OK)
			{
				Log.Debug("User canceled browse");
				return;
			}

			Folder.Text = dialog.SelectedPath;
		}
	}
}
