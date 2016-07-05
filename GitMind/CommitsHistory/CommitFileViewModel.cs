using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class CommitFileViewModel : ViewModel
	{
		private IDiffService diffService = new DiffService();

		public string Id { get; set; }

		public bool HasParentCommit { get; set; }

		public string Name
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string Status
		{
			get { return Get(); }
			set { Set(value); }
		}

		public Command ShowDiffCommand => Command(ShowDiffAsync);

		private void ShowDiffAsync()
		{
			diffService.ShowFileDiffAsync(Id, Name).RunInBackground();
		}
	}
}