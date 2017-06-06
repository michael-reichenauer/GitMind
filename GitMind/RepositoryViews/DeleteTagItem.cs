using System.Threading.Tasks;
using GitMind.Features.Tags;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class DeleteTagItem : ViewModel
	{
		private readonly ITagService tagService;


		public DeleteTagItem(ITagService tagService, string text)
		{
			this.tagService = tagService;
			Text = text;
		}

		public Command DeleteTagCommand => AsyncCommand(DeleteTagAsync);

		public string Text { get; }

		private async Task DeleteTagAsync()
		{
			await tagService.DeleteTagAsync(Text);
		}
	}
}