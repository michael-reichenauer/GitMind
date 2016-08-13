using System;
using System.Threading.Tasks;
using System.Windows;
using GitMind.Utils;


namespace GitMind.Common
{
	public interface IProgress
	{
		
	}


	/// <summary>
	/// Interaction logic for ProgressDialog.xaml
	/// </summary>
	public partial class ProgressDialog : Window, IProgress
	{
		private readonly ProgressDialogViewModel viewModel;
		private readonly Func<IProgress, Task<object>> progressAction;
		private object result;

		private ProgressDialog(Window owner, string text, Func<IProgress, Task<object>> progressAction)
		{
			this.progressAction = progressAction;
			Owner = owner;
			InitializeComponent();

			viewModel = new ProgressDialogViewModel();
			viewModel.Text = text;
			DataContext = viewModel;
		}


		public static void Show(Window owner, string text, Func<Task> progressAction)
		{
			Show<object>(
				owner,
				text,
				async _ =>
				{
					await progressAction();
					return null;
			});
		}


		public static T Show<T>(Window owner, string text, Func<Task<T>> progressAction)
		{
			return Show(owner, text, async _ => await progressAction());
		}


		public static T Show<T>(Window owner, Func<IProgress, Task<T>> progressAction)
		{
			return Show(owner, null, async progress => await progressAction(progress));
		}


		private static T Show<T>(Window owner, string text, Func<IProgress, Task<T>> progressAction)
		{
			ProgressDialog progressDialog = new ProgressDialog(
				owner, text, async progress => await progressAction(progress));

			progressDialog.ShowDialog();
			return (T)progressDialog.result;
		}


		private async void ProgressDialog_OnLoaded(object sender, RoutedEventArgs e)
		{
			viewModel.Start();
			try
			{
				result = await progressAction(this);
			}
			catch (Exception ex) when (ex.IsNotFatal())
			{
				Log.Warn($"Exception {ex}");
			}

			viewModel.Stop();
			DialogResult = true;
		}
	}
}
