using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class CommitDetailViewModel : ViewModel
	{
		private readonly Func<string, Task> showDiffAsync;


		public CommitDetailViewModel(
			Func<string, Task> showDiffAsync)
		{
			this.showDiffAsync = showDiffAsync;
		}


		public ObservableCollection<CommitFileViewModel> Files { get; }
			= new ObservableCollection<CommitFileViewModel>();

		public string Id
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string Branch
		{
			get { return Get(); }
			set { Set(value); }
		}

		public Brush BranchBrush
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string Subject
		{
			get { return Get(); }
			set { Set(value); }
		}

		public Brush SubjectBrush
		{
			get { return Get(); }
			set { Set(value); }
		}

		public FontStyle SubjectStyle
		{
			get { return Get<FontStyle>(); }
			set { Set(value); }
		}


		public string Tags
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string Tickets
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string BranchTips
		{
			get { return Get(); }
			set { Set(value); }
		}

		public Command ShowDiffCommand => Command(ShowDiffAsync);

		public override string ToString() => $"{Id} {Subject}";


		private async void ShowDiffAsync()
		{
			await showDiffAsync(Id);
		}
	}
}